using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartHome.Domain;
using SmartHome.Infrastructure.Data;

namespace SmartHome.Application.Sensors.GetCurrentStatus;

public class GetCurrentStatusQueryHandler(SmartHomeDbContext db)
    : IRequestHandler<GetCurrentStatusQuery, CurrentStatusResult>
{
    public async Task<CurrentStatusResult> Handle(GetCurrentStatusQuery request, CancellationToken cancellationToken)
    {
        var sensors = await db.Sensors.ToListAsync(cancellationToken);
        var readings = await db.SensorReadings.ToListAsync(cancellationToken);

        // Same SQLite/DateTimeOffset limitation as GetSensorHistoryQuery: group and sort in
        // memory rather than asking the database to order by DateTimeOffset.
        var latestBySensor = readings
            .GroupBy(r => r.SensorId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.Timestamp).First());

        var statuses = sensors
            .Select(s =>
            {
                latestBySensor.TryGetValue(s.Id, out var latest);
                return new SensorStatus(s.Id, s.Name, s.Room, s.Type, latest?.Value, latest?.Unit, latest?.Timestamp);
            })
            .ToList();

        bool anythingHot = statuses.Any(s => s.Type == SensorType.Temperature && s.LatestValue > 27);

        return new CurrentStatusResult(statuses, anythingHot);
    }
}
