
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Messaging;
using GameHub.Abstractions.Primitives;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using Microsoft.EntityFrameworkCore;


namespace GameHub.Application.Features.Chats.Queries.GetParticipantsCount;

public static partial class GetParticipantCountByChat
{
    public sealed class Handler : IQueryHandler<Query, int>
    {
        private readonly IApplicationDbContext _context;

        public Handler(IApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Result<int>> Handle(Query request, CancellationToken cancellationToken)
        {
            var chatExists = await _context.Chats
                .AnyAsync(
                    r => r.Id == request.ChatId, 
                    cancellationToken
                 );

            if (!chatExists)
            {
                return Result.Failure<int>(ChatErrors.ChatGroupNotFound(request.ChatId));
            }

            return await _context.ChatMembers
                .Where(r => r.ChatId == request.ChatId)
                .AsNoTracking()
                .CountAsync(cancellationToken);
        }
    }
}
