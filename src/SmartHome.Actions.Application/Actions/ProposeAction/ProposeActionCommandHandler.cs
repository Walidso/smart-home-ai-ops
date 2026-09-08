using MediatR;
using SmartHome.Actions.Domain;
using SmartHome.Actions.Infrastructure.Data;

namespace SmartHome.Actions.Application.Actions.ProposeAction;

public class ProposeActionCommandHandler(ActionsDbContext db) : IRequestHandler<ProposeActionCommand, ProposedActionDto>
{
    public async Task<ProposedActionDto> Handle(ProposeActionCommand request, CancellationToken cancellationToken)
    {
        var action = new ProposedAction
        {
            Description = request.Description,
            TargetRoom = request.TargetRoom,
        };

        db.ProposedActions.Add(action);
        await db.SaveChangesAsync(cancellationToken);

        return new ProposedActionDto(
            action.Id, action.Description, action.TargetRoom,
            action.Status.ToString(), action.ProposedAt, action.DecidedAt);
    }
}
