using FluentValidation;

namespace GameHub.Application.Features.Presence.Commands.Update;

public static partial class UpdatePresence
{
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ChatId)
                .NotEmpty();
        }
    }
}