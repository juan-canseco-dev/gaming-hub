using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Messaging;
using GameHub.Contracts.Profile;
using GameHub.Domain.Chats;
using GameHub.Abstractions.Primitives;
using Microsoft.EntityFrameworkCore;
using GameHub.Abstractions.Pagination;

namespace GameHub.Application.Features.Chats.Queries.GetParticipants;

public static partial class GetChatParticipants
{
    public sealed class Handler : IQueryHandler<Query, CursorPage<UserDto>>
    {
        private readonly IApplicationDbContext _context;

        public Handler(IApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
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

            var query = _context.ChatMembers
                .AsNoTracking()
                .Where(x => x.ChatId == request.ChatId)
                .ProjectToDto(_context.UserProfiles.AsNoTracking());

            if (cursor is not null)
            {
                query = query.Where(x =>
                   x.Username.CompareTo(cursor.Username) > 0 ||
                   (x.Username == cursor.Username && x.Id.CompareTo(cursor.UserId) > 0)
                );
            }

            var items = await query
                .OrderBy(x => x.Username)
                .ThenBy(x => x.Id)
                .Take(request.Limit + 1)
                .ToListAsync(cancellationToken);

            var hasMore = items.Count > request.Limit;

            if (hasMore)
            {
                items.RemoveAt(items.Count - 1);
            }

            string? afterCursor = hasMore
                ? Cursor.Encode(items[^1].Username, items[^1].Id)
                : null;


            return Result.Success(new CursorPage<UserDto>
            {
                Items = items,
                Next = afterCursor
            });

        }
    }
}