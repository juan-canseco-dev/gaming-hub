using GameHub.Abstractions.Primitives;
using GameHub.Application.Abstractions.Authentication;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Messaging;
using GameHub.Contracts.Chats;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Application.Features.Chats.Queries.GetById;
public static partial class GetChatById
{
    public sealed class Handler : IQueryHandler<Query, ChatDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuthenticatedUserService _authService;

        public Handler(
            IApplicationDbContext context, 
            IAuthenticatedUserService authService
        )
        {
            _context = context ?? throw new ArgumentNullException( nameof( context ) );
            _authService = authService ?? throw new ArgumentException( nameof( authService ) );
        }

        public async Task<Result<ChatDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var chat = await _context.ChatMembers.AsNoTracking()
                .Where(x=> x.UserId == _authService.UserId &&  x.ChatId == request.ChatId)
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
                       cm.LastReadAt == null
                       ? m.SenderUserId != _authService.UserId
                       : m.CreatedAt > cm.LastReadAt && m.SenderUserId != _authService.UserId
                    )
                 ))
                .FirstOrDefaultAsync(cancellationToken);

            if (chat is null)
            {
                return Result.Failure<ChatDto>(ChatErrors.ChatGroupNotFound(request.ChatId));
            }

            return chat;
        }
    }
}
