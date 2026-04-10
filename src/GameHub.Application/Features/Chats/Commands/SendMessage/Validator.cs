using FluentValidation;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;

namespace GameHub.Application.Features.Chats.Commands.SendMessage;

public static partial class ChatSendMessage
{
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ChatId)
                .NotEmpty();

            RuleFor(x => x.Content)
                .NotEmpty()
                .MaximumLength(Chat.MaxMessageLength);
        }
    }
}
