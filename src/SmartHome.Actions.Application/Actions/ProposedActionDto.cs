namespace SmartHome.Actions.Application.Actions;

// Status as its string name (e.g. "Pending") rather than the raw enum int — makes the JSON
// self-explanatory to a client without it needing to know what "0" means.
public record ProposedActionDto(
    Guid Id,
    string Description,
    string? TargetRoom,
    string Status,
    DateTimeOffset ProposedAt,
    DateTimeOffset? DecidedAt);
