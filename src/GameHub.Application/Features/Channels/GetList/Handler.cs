using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Messaging;
using GameHub.Contracts.Channels;
using GameHub.Abstractions.Primitives;
using Microsoft.EntityFrameworkCore;
using GameHub.Application.Abstractions.Authentication;

namespace GameHub.Application.Features.Channels.GetList;

public static partial class GetChannels
{
    public sealed class Handler : IQueryHandler<Query, IReadOnlyCollection<ChannelDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuthenticatedUserService _authService;

        public Handler(IApplicationDbContext context, IAuthenticatedUserService authService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        public async Task<Result<IReadOnlyCollection<ChannelDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            return await _context.Chats
                .AsNoTracking()
                .ApplySortingByChannel()
                .ProjectToResponse(_context.ChatMembers, _authService.UserId)
                .ToListAsync(cancellationToken);
        }
    }
}