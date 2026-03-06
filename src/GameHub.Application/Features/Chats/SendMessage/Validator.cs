using FluentValidation;
using GameHub.Domain.Chats;

namespace GameHub.Application.Features.Chats.SendMessage;
public static partial class ChatSendMessage
{
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ChatId)
                .NotEmpty();

            RuleFor(x => x.UserId)
                .NotEmpty();

            RuleFor(x => x.Content)
                .NotEmpty()
                .MaximumLength(Chat.MaxMessageLength);
        }
    }
}
