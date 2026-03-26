using GameHub.Abstractions.Primitives;
using GameHub.Application.Abstractions.Authentication;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Messaging;
using GameHub.Contracts.Chats;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Application.Features.Chats.Queries.GetMyChats;

public static partial class GetUserChats 
{
    public sealed class Handler : IQueryHandler<Query, IReadOnlyCollection<ChatDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuthenticatedUserService _authService;

        public Handler(
            IApplicationDbContext context, 
            IAuthenticatedUserService authService
        )
        {
            _context = context ?? throw new ArgumentNullException( nameof( context ) );
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        public async Task<Result<IReadOnlyCollection<ChatDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            return await _context.ChatMembers.AsNoTracking()
                .Where(x => x.UserId == _authService.UserId)
                .OrderByDescending(x => x.Chat.LastMessageAt)
                .Select(cm => new ChatDto(
                    cm.ChatId,
                    cm.Chat.ChannelId,
                    cm.Chat.Channel.Slug,
                    cm.Chat.Channel.Name,
                    cm.Chat.Channel.Description,
                    cm.Chat.Members.Count(),
                    cm.Chat.LastMessagePreview,
                    cm.Chat.LastMessageAt,
                    cm.Chat.Messages.Count(m =>
                        m.CreatedAt > cm.LastReadAt &&
                        m.SenderUserId != _authService.UserId
                    )
                 ))
                .ToListAsync(cancellationToken);
        }
    }
}
