using FluentValidation;
using FluentValidation.Results;
using GameHub.Application.Abstractions.Messaging;
using GameHub.Application.Exceptions;
using MediatR;


namespace GameHub.Application.Abstractions.Behaviors;
public class ValidationBehavior<TRequest, TResponse>
: IPipelineBehavior<TRequest, TResponse> where TRequest : IBaseCommand
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationErrors = new List<ValidationFailure>();

        foreach (var validator in _validators)
        {
            var validationResult = await validator.ValidateAsync(context, cancellationToken);
            if (validationResult.Errors.Any())
            {
                validationErrors.AddRange(validationResult.Errors);
            }
        }

        if (validationErrors.Any())
        {
            var errors = validationErrors
                .Select(validationFailure => new ValidationError(
                    validationFailure.PropertyName,
                    validationFailure.ErrorMessage
                )).ToList();

            throw new Exceptions.ValidationException(errors);
        }

        return await next();
    }
}