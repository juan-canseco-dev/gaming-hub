
using GameHub.Abstractions.Primitives;
using GameHub.Application.Abstractions.Authentication;
using GameHub.Application.Abstractions.Clock;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Messaging;

namespace GameHub.Application.Features.Presence.Commands.Update;

public static partial class UpdatePresence
{
    public sealed class Handler : ICommandHandler<Command>
    {
        private readonly IAuthenticatedUserService _authService;
        private readonly IApplicationDbContext _context;
        private readonly IDateTimeProvider _timeProvider;

        public Handler(
            IAuthenticatedUserService authService, 
            IApplicationDbContext context, 
            IDateTimeProvider timeProvider)
        {
            _authService = authService  ?? throw new ArgumentNullException(nameof(authService));
            _context = context ?? throw new ArgumentNullException(nameof(context)); 
            _timeProvider = timeProvider  ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var currentUserId = _authService.UserId;


            // TODO : Implement presence update logic, for now we just return success




            return Result.Success();

        }
    }
}
