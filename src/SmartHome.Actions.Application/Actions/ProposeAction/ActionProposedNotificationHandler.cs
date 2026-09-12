using MediatR;

namespace SmartHome.Actions.Application.Actions.ProposeAction;

public class ActionProposedNotificationHandler(IActionEventPublisher publisher) : INotificationHandler<ActionProposedNotification>
{
    public Task Handle(ActionProposedNotification notification, CancellationToken cancellationToken) =>
        publisher.PublishActionProposedAsync(
            new ActionProposedMessage(notification.Id, notification.Description, notification.TargetRoom, notification.ProposedAt),
            cancellationToken);
}
