using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Messaging;
using GameHub.Contracts.Chats;
using GameHub.Abstractions.Primitives;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using Microsoft.EntityFrameworkCore;
using GameHub.Application.Abstractions.Authentication;

namespace GameHub.Application.Features.Chats.Queries.GetMessage;

public static partial class GetMessageById
{
    public sealed class Handler : IQueryHandler<Query, MessageDto>
    {
        private readonly IApplicationDbContext _context;
        public Handler(
            IApplicationDbContext context
        )
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Result<MessageDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var message = await _context.ChatMessages
                .Where(r => r.Id == request.MessageId)
                .ProjectToDto(_context.UserProfiles)
                .FirstOrDefaultAsync(cancellationToken);

            if (message == null)
            {
                return Result.Failure<MessageDto>(MessageErrors.NotFound(request.MessageId));
            }

            return message;
        }
    }
}
