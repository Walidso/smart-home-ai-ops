namespace SmartHome.Actions.Application.Actions;

// Shared by ApproveActionCommand and RejectActionCommand. Business outcomes like "already
// decided" are expected, everyday results — not exceptional — so they're modeled as a plain
// return value the endpoint switches on, rather than as thrown exceptions.
public enum DecisionOutcome
{
    Success,
    NotFound,
    AlreadyDecided
}
