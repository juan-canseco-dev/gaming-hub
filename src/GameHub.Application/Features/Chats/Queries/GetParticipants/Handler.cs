using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Messaging;
using GameHub.Contracts.Profile;
using GameHub.Domain.Chats;
using GameHub.Abstractions.Primitives;
using Microsoft.EntityFrameworkCore;
using GameHub.Abstractions.Pagination;
using GameHub.Application.Abstractions.Clock;
using static GameHub.Contracts.Chats.ChatParticipantCursor;
using GameHub.Contracts.Presence;

namespace GameHub.Application.Features.Chats.Queries.GetParticipants;

public static partial class GetChatParticipants
{
    public sealed class Handler : IQueryHandler<Query, CursorPage<UserDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IDateTimeProvider _timeProvider;

        public Handler(IApplicationDbContext context, IDateTimeProvider timeProvider)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public async Task<Result<CursorPage<UserDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            Cursor? cursor = null;

            var chatExists = await _context.Chats
                .AsNoTracking()
                .AnyAsync(x => x.Id == request.ChatId, cancellationToken);

            if (!chatExists)
            {
                return Result.Failure<CursorPage<UserDto>>(
                    ChatErrors.ChatGroupNotFound(request.ChatId));
            }

            if (!string.IsNullOrWhiteSpace(request.Cursor))
            {
                var decodeCursorResult = Cursor.Decode(request.Cursor);
                if (decodeCursorResult.IsFailure)
                {
                    return Result.Failure<CursorPage<UserDto>>(Errors.InvalidCursor);
                }

                cursor = decodeCursorResult.Value;
            }

            var currentTime = _timeProvider.CurrentTimeUtc;
            var query = _context.ChatMembers
                .AsNoTracking()
                .Where(x => x.ChatId == request.ChatId)
                .ProjectToParticipant(
                    _context.UserProfiles.AsNoTracking(),
                    _context.UserPresences.AsNoTracking());

            if (cursor is not null)
            {
                if (!cursor.LastActive.HasValue)
                {
                    return Result.Failure<CursorPage<UserDto>>(Errors.InvalidCursor);
                }

                query = query.Where(x =>
                    x.Presence.LastActive < cursor.LastActive.Value ||
                    (x.Presence.LastActive == cursor.LastActive.Value &&
                        (x.Username.CompareTo(cursor.Username) > 0 ||
                         (x.Username == cursor.Username && x.Id.CompareTo(cursor.UserId) > 0))));
            }

            var items = await query
                .OrderByDescending(x => x.Presence.LastActive)
                .ThenBy(x => x.Username)
                .ThenBy(x => x.Id)
                .Take(request.Limit + 1)
                .ToListAsync(cancellationToken);

            var hasMore = items.Count > request.Limit;

            if (hasMore)
            {
                items.RemoveAt(items.Count - 1);
            }

            string? afterCursor = hasMore
                ? Cursor.Encode(items[^1].Presence.LastActive, items[^1].Username, items[^1].Id)
                : null;

            var participants = items.Select(x => new UserDto
            {
                Id = x.Id,
                Username = x.Username,
                Email = x.Email,
                Fullname = x.Fullname,
                Presence = new UserPresenceDto(
                    x.Id,
                    x.Presence.LastActive,
                    x.Presence.GetStatus(currentTime).Name)
            }).ToArray();

            return Result.Success(new CursorPage<UserDto>
            {
                Items = participants,
                Next = afterCursor
            });

        }
    }
}
