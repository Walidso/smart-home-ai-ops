using MediatR;

namespace SmartHome.Actions.Application.Actions.ListPendingApprovals;

// No parameters, so no validator — same reasoning as GetCurrentStatusQuery on the Sensors side.
public record ListPendingApprovalsQuery : IRequest<IReadOnlyList<ProposedActionDto>>;
