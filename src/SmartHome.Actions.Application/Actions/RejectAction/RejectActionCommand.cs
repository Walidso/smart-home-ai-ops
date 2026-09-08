using MediatR;

namespace SmartHome.Actions.Application.Actions.RejectAction;

public record RejectActionCommand(Guid ActionId) : IRequest<DecisionOutcome>;
