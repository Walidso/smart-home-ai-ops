namespace SmartHome.Actions.Application.Actions;

public record ActionProposedMessage(Guid Id, string Description, string? TargetRoom, DateTimeOffset ProposedAt);

// Defined here (Application) but implemented in the Api project, using RabbitMQ — the
// Application layer shouldn't need to know or care which message broker (or whether one at
// all) actually delivers this. That's dependency inversion: the inner layer declares what it
// needs, the outer layer provides it.
public interface IActionEventPublisher
{
    Task PublishActionProposedAsync(ActionProposedMessage message, CancellationToken cancellationToken);
}
