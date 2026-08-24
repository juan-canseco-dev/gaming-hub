
using GameHub.Abstractions.Primitives;
using GameHub.Application.Abstractions.Clock;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Messaging;
using GameHub.Domain.Chats;
using GameHub.Domain.Presence;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Application.Features.Presence.Queries.GetOnlineUsersCount;

public static partial class GetOnlineUsersCount
{
    public sealed class Handler : IQueryHandler<Query, int>
    {
        private readonly IApplicationDbContext _context;
        private readonly IDateTimeProvider _timeProvider;

        public Handler(
            IApplicationDbContext context,
            IDateTimeProvider timeProvider)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public async Task<Result<int>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!await _context.Chats.AnyAsync(x => x.Id == request.ChatId, cancellationToken))
            {
                return Result.Failure<int>(ChatErrors.ChatGroupNotFound(request.ChatId));
            }
            
            var onlineCutoff = UserPresence.GetOnlineCutoff(_timeProvider.CurrentTimeUtc);

            var query =
                from member in _context.ChatMembers.AsNoTracking()
                join presence in _context.UserPresences.AsNoTracking()
                    on member.UserId equals presence.UserId
                where member.ChatId == request.ChatId && presence.LastActive >= onlineCutoff
                select member;

            return await query.CountAsync(cancellationToken);
        }
    }
}
