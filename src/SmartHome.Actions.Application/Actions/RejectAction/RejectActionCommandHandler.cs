using MediatR;
using SmartHome.Actions.Domain;
using SmartHome.Actions.Infrastructure.Data;

namespace SmartHome.Actions.Application.Actions.RejectAction;

public class RejectActionCommandHandler(ActionsDbContext db) : IRequestHandler<RejectActionCommand, DecisionOutcome>
{
    public async Task<DecisionOutcome> Handle(RejectActionCommand request, CancellationToken cancellationToken)
    {
        var action = await db.ProposedActions.FindAsync([request.ActionId], cancellationToken);
        if (action is null)
            return DecisionOutcome.NotFound;

        if (action.Status != ActionStatus.Pending)
            return DecisionOutcome.AlreadyDecided;

        action.Reject();
        await db.SaveChangesAsync(cancellationToken);

        return DecisionOutcome.Success;
    }
}
