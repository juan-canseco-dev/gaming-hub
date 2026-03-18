using FluentValidation;

namespace GameHub.Application.Features.Chats.Queries.GetParticipants;

public static partial class GetChatParticipants
{
    public class Validator : AbstractValidator<Query>
    {
        public const int MinLimit = 1;
        public const int MaxLimit = 100;
        public Validator()
        {
            RuleFor(x => x.ChatId)
                .NotEmpty();

            RuleFor(x => x.Limit)
                .InclusiveBetween(MinLimit, MaxLimit);
        }
    }
}
