using FluentValidation;

namespace GameHub.Application.Features.Chats.Commands.Join;

public static partial class JoinChat
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ChatId)
                .NotEmpty();
        }
    }
}
