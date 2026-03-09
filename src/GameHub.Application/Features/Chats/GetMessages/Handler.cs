using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Messaging;
using GameHub.Application.Contracts.Chats;
using GameHub.Domain.Abstractions;
using GameHub.Domain.Chats;
using GameHub.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Application.Features.Chats.GetMessages;

public static partial class GetMessagesByChat
{
    public sealed class Handler : IQueryHandler<Query, CursorPage<MessageDto>>
    {
        private readonly IApplicationDbContext _context;

        public Handler(IApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Result<CursorPage<MessageDto>>> Handle(Query request, CancellationToken cancellationToken)
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

            var query = _context.ChatMessages
                .AsNoTracking()
                .Where(x => x.ChatId == request.ChatId);

            if (cursor != null) 
            {
                query = query.Where(
                    x => x.CreatedAt < cursor.CreatedAt ||
                    (x.CreatedAt == cursor.CreatedAt && x.Id < cursor.MessageId)
                );
            }

            var items = await query
               .OrderByDescending(x => x.CreatedAt)
               .ThenByDescending(x => x.Id)
               .Take(request.Limit + 1)
               .ProjectToDto(_context.UserProfiles)
               .ToListAsync(cancellationToken);

            bool hasMore = items.Count > request.Limit;

            string? afterCursor = hasMore ? Cursor.Encode(items[^1].CreatedAt, items[^1].Id) : null;
            string? beforeCursor = items.Count > 0 ? Cursor.Encode(items[0].CreatedAt, items[0].Id) : null;

            if (hasMore)
            {
                items.RemoveAt(items.Count - 1);
            }


            return Result.Success(new CursorPage<MessageDto>
            {
                Items = items,
                After = afterCursor,
                Before = beforeCursor
            });
        }
    }
}
