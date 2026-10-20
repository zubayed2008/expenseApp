using FluentValidation;
using MediatR;

namespace ExpenseTracker.Service.Behaviors;

/// <summary>
/// Runs all FluentValidation validators for a request before its handler.
/// Validation failures surface as ValidationException, mapped to a 400
/// ProblemDetails by the WebCore exception middleware.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var failures = validators
                .Select(validator => validator.Validate(context))
                .SelectMany(result => result.Errors)
                .Where(failure => failure is not null)
                .ToList();

            if (failures.Count > 0)
            {
                throw new ValidationException(failures!);
            }
        }

        return await next();
    }
}
