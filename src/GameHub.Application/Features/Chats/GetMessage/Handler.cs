using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Messaging;
using GameHub.Application.Contracts.Chats;
using GameHub.Domain.Abstractions;
using GameHub.Domain.Chats;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Application.Features.Chats.GetMessage;

public static partial class GetMessageById
{
    public sealed class Handler : IQueryHandler<Query, MessageDto>
    {
        private readonly IApplicationDbContext _context;
        public Handler(IApplicationDbContext context)
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
