using FluentValidation;
using MediatR;

namespace SmartHome.Actions.Application.Behaviors;

// Deliberately a near-duplicate of SmartHome.Application.Behaviors.ValidationBehavior rather
// than a shared library reference — this service and the Sensors service are meant to be
// independently buildable/deployable, so they don't share internal code across the service
// boundary, even for small cross-cutting pieces like this one. A little duplication here beats
// coupling two services' release cycles together.
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
