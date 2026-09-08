using MediatR;

namespace SmartHome.Actions.Application.Actions.ApproveAction;

public record ApproveActionCommand(Guid ActionId) : IRequest<DecisionOutcome>;
