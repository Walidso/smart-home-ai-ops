using FluentValidation;
using MediatR;

namespace SmartHome.Application.Behaviors;

// Runs before every MediatR request's handler. If any FluentValidation validators are
// registered for this request type, it runs them all and throws ValidationException on
// failure — the handler never even gets called. Requests with no registered validator (like
// GetCurrentStatusQuery) just pass straight through.
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
