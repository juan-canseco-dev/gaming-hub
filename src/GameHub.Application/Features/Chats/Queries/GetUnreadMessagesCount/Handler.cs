using GameHub.Abstractions.Primitives;
using GameHub.Application.Abstractions.Authentication;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Messaging;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Application.Features.Chats.Queries.GetUnreadMessagesCount;
public static partial class GetUnreadMessagesCountByChat
{
    public sealed class Handler : IQueryHandler<Query, int>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuthenticatedUserService _authService;

        public Handler(IApplicationDbContext context, IAuthenticatedUserService authService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        public async Task<Result<int>> Handle(Query request, CancellationToken cancellationToken)
        {
            var userId = _authService.UserId;

            var chatExists = await _context.Chats
                .AsNoTracking()
                .AnyAsync(
                    r => r.Id == request.ChatId,
                    cancellationToken
                 );

            if (!chatExists)
            {
                return Result.Failure<int>(ChatErrors.ChatGroupNotFound(request.ChatId));
            }

            var membership = await _context
                .ChatMembers
                .AsNoTracking()
                .Where(x => x.ChatId == request.ChatId && x.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (membership is null)
            {
                return Result.Failure<int>(ChatErrors.NotParticipant(userId));
            }

            return await _context.ChatMessages
                .AsNoTracking()
                .Where(x => x.ChatId == request.ChatId)
                .Where(x => 
                    membership.LastReadAt == null
                    ? x.SenderUserId != userId
                    : x.CreatedAt > membership.LastReadAt && x.SenderUserId != userId
                 )
                .CountAsync(cancellationToken);
        }
    }
}
