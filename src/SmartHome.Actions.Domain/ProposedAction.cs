namespace SmartHome.Actions.Domain;

public class ProposedAction
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Description { get; init; }
    public string? TargetRoom { get; init; }
    public ActionStatus Status { get; private set; } = ActionStatus.Pending;
    public DateTimeOffset ProposedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DecidedAt { get; private set; }

    public void Approve() => Decide(ActionStatus.Approved);

    public void Reject() => Decide(ActionStatus.Rejected);

    private void Decide(ActionStatus decision)
    {
        if (Status != ActionStatus.Pending)
            throw new InvalidOperationException($"Cannot {decision.ToString().ToLowerInvariant()} an action that is already {Status}.");

        Status = decision;
        DecidedAt = DateTimeOffset.UtcNow;
    }
}
