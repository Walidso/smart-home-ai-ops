using MediatR;

namespace SmartHome.Actions.Application.Actions.ProposeAction;

public record ProposeActionCommand(string Description, string? TargetRoom) : IRequest<ProposedActionDto>;
