using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartHome.Infrastructure.Data;

namespace SmartHome.Application.Sensors.GetSensorHistory;

public class GetSensorHistoryQueryHandler(SmartHomeDbContext db)
    : IRequestHandler<GetSensorHistoryQuery, IReadOnlyList<SensorReadingDto>?>
{
    public async Task<IReadOnlyList<SensorReadingDto>?> Handle(GetSensorHistoryQuery request, CancellationToken cancellationToken)
    {
        var sensorExists = await db.Sensors.AnyAsync(s => s.Id == request.SensorId, cancellationToken);
        if (!sensorExists)
            return null;

        // SQLite's EF Core provider can't translate ORDER BY on a DateTimeOffset column into SQL,
        // so we pull the rows first and sort them in memory instead of asking the database to.
        var readings = await db.SensorReadings
            .Where(r => r.SensorId == request.SensorId)
            .Select(r => new SensorReadingDto(r.Id, r.Timestamp, r.Value, r.Unit))
            .ToListAsync(cancellationToken);

        return readings.OrderByDescending(r => r.Timestamp).ToList();
    }
}
