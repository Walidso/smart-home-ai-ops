using MediatR;
using SmartHome.Actions.Domain;
using SmartHome.Actions.Infrastructure.Data;

namespace SmartHome.Actions.Application.Actions.ApproveAction;

public class ApproveActionCommandHandler(ActionsDbContext db) : IRequestHandler<ApproveActionCommand, DecisionOutcome>
{
    public async Task<DecisionOutcome> Handle(ApproveActionCommand request, CancellationToken cancellationToken)
    {
        var action = await db.ProposedActions.FindAsync([request.ActionId], cancellationToken);
        if (action is null)
            return DecisionOutcome.NotFound;

        if (action.Status != ActionStatus.Pending)
            return DecisionOutcome.AlreadyDecided;

        action.Approve();
        await db.SaveChangesAsync(cancellationToken);

        return DecisionOutcome.Success;
    }
}
