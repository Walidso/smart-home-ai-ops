using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartHome.Actions.Domain;
using SmartHome.Actions.Infrastructure.Data;

namespace SmartHome.Actions.Application.Actions.ListPendingApprovals;

public class ListPendingApprovalsQueryHandler(ActionsDbContext db)
    : IRequestHandler<ListPendingApprovalsQuery, IReadOnlyList<ProposedActionDto>>
{
    public async Task<IReadOnlyList<ProposedActionDto>> Handle(ListPendingApprovalsQuery request, CancellationToken cancellationToken)
    {
        // Same lesson as before: pull the matching rows first, then map to DTOs (including the
        // enum-to-string conversion) in memory rather than inside the LINQ query — SQL providers
        // generally can't translate Enum.ToString() into SQL.
        var pending = await db.ProposedActions
            .Where(a => a.Status == ActionStatus.Pending)
            .ToListAsync(cancellationToken);

        return pending
            .OrderBy(a => a.ProposedAt)
            .Select(a => new ProposedActionDto(a.Id, a.Description, a.TargetRoom, a.Status.ToString(), a.ProposedAt, a.DecidedAt))
            .ToList();
    }
}
