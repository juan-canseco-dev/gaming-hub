using GameHub.Abstractions.Primitives;
using GameHub.Application.Abstractions.Authentication;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;


namespace GameHub.Application.Features.Chats.Queries.GetTotalUnreadMessagesCount;

public static partial class GetTotalChatUnreadMessagesCount
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
            return await 
                (from member in _context.ChatMembers
                 join message in _context.ChatMessages
                     on member.ChatId equals message.ChatId
                 where member.UserId == userId
                       && (message.CreatedAt > member.LastReadAt || member.LastReadAt == null) 
                       && message.SenderUserId != userId
                 select message.Id)
                .CountAsync(cancellationToken);
        }
    }
}