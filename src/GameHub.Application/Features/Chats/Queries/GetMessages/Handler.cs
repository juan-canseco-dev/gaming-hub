using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Messaging;
using GameHub.Contracts.Chats;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using GameHub.Abstractions.Primitives;
using Microsoft.EntityFrameworkCore;
using GameHub.Abstractions.Pagination;
using static GameHub.Contracts.Chats.ChatMessageCursor;

namespace GameHub.Application.Features.Chats.Queries.GetMessages;

public static partial class GetMessagesByChat
{
    public sealed class Handler : IQueryHandler<Query, CursorPage<MessageDto>>
    {
        private readonly IApplicationDbContext _context;

        public Handler(
            IApplicationDbContext context
        )
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Result<CursorPage<MessageDto>>> Handle(
           Query request,
           CancellationToken cancellationToken
        )
        {
            Cursor? cursor = null;

            var chatExists = await _context.Chats
                .AsNoTracking()
                .AnyAsync(x => x.Id == request.ChatId, cancellationToken);

            if (!chatExists)
            {
                return Result.Failure<CursorPage<MessageDto>>(
                    ChatErrors.ChatGroupNotFound(request.ChatId));
            }

            if (!string.IsNullOrWhiteSpace(request.Cursor))
            {
                var decodeCursorResult = Cursor.Decode(request.Cursor);
                if (decodeCursorResult.IsFailure)
                {
                    return Result.Failure<CursorPage<MessageDto>>(Errors.InvalidCursor);
                }

                cursor = decodeCursorResult.Value;
            }

            var baseQuery = _context.ChatMessages
                .AsNoTracking()
                .Where(x => x.ChatId == request.ChatId);

            if (cursor is not null)
            {
                baseQuery = baseQuery.Where(x =>
                    x.CreatedAt < cursor.CreatedAt ||
                    (x.CreatedAt == cursor.CreatedAt && x.Id < cursor.MessageId));
            }

            var items = await baseQuery
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(request.Limit + 1)
                .ProjectToDto(_context.UserProfiles)
                .ToListAsync(cancellationToken);

            var hasMore = items.Count > request.Limit;

            if (hasMore)
            {
                items.RemoveAt(items.Count - 1);
            }

            string? nextCursor = null;
            if (hasMore && items.Count > 0)
            {
                var lastVisibleItem = items[^1];
                nextCursor = Cursor.Encode(lastVisibleItem.CreatedAt, lastVisibleItem.Id);
            }

            return Result.Success(new CursorPage<MessageDto>
            {
                Items = items,
                Next = nextCursor
            });
        }

       
    }
}
