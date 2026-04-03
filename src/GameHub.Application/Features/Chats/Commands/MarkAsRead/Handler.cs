using GameHub.Abstractions.Primitives;
using GameHub.Application.Abstractions.Authentication;
using GameHub.Application.Abstractions.Clock;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Messaging;
using GameHub.Domain.Chats;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Application.Features.Chats.Commands.MarkAsRead;

public static partial class MarkChatAsRead
{
    public sealed class Handler : ICommandHandler<Command>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuthenticatedUserService _authService;
        private readonly IDateTimeProvider _timeProvider;

        public Handler(
            IApplicationDbContext context, 
            IAuthenticatedUserService authService,
            IDateTimeProvider timeProvider
        )
        {
            _context = context ?? throw new ArgumentNullException( nameof( context ) );
            _authService = authService ?? throw new ArgumentException(nameof(authService));
            _timeProvider = timeProvider ?? throw new ArgumentNullException( nameof( timeProvider ) );
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var userId = _authService.UserId;
            var readUpTo = _timeProvider.CurrentTimeUtc;

            var chatExists = await _context.Chats
                .AsNoTracking()
                .AnyAsync(
                    r => r.Id == request.ChatId,
                    cancellationToken
                 );

            if (!chatExists)
            {
                return Result.Failure(ChatErrors.ChatGroupNotFound(request.ChatId));
            }

            var membership = await _context
                .ChatMembers
                .Where(x => x.ChatId == request.ChatId && x.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (membership is null)
            {
                return Result.Failure(ChatErrors.NotParticipant(userId));
            }

            membership.ReadUpTo(readUpTo);

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
