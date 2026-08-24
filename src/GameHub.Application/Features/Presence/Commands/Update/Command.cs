using GameHub.Application.Abstractions.Messaging;

namespace GameHub.Application.Features.Presence.Commands.Update;

public static partial class UpdatePresence
{
    public sealed record Command : ICommand;
}
