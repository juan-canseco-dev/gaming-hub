using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Messaging;
using GameHub.Application.Contracts.Channels;
using GameHub.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameHub.Application.Features.Channels.GetList;

public static partial class GetChannels
{
    internal sealed class Handler : IQueryHandler<Query, IReadOnlyCollection<ChannelDto>>
    {
        private readonly IApplicationDbContext _context;

        public Handler(IApplicationDbContext context, ILogger<Handler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Result<IReadOnlyCollection<ChannelDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            return await _context.Chats
                .AsNoTracking()
                .ApplySortingByChannel()
                .ProjectToResponse()
                .ToListAsync(cancellationToken);
        }
    }
}