namespace SmartHome.Notify;

// Matches the JSON shape SmartHome.Actions.Api publishes — deliberately not a shared type
// between the two services, same reasoning as the Sensors/Actions message contract.
public record ActionProposedMessage(Guid Id, string Description, string? TargetRoom, DateTimeOffset ProposedAt);
