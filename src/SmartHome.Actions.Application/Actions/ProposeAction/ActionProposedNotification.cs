using MediatR;

namespace SmartHome.Actions.Application.Actions.ProposeAction;

// An in-process MediatR notification (INotification) — distinct from a RabbitMQ message.
// Multiple handlers inside this same app could react to it independently; here there's only
// one, which forwards it out over RabbitMQ for SmartHome.Notify to pick up.
public record ActionProposedNotification(Guid Id, string Description, string? TargetRoom, DateTimeOffset ProposedAt) : INotification;
