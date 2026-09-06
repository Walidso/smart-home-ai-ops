using MediatR;

namespace SmartHome.Application.Sensors.GetSensorHistory;

public record SensorReadingDto(Guid Id, DateTimeOffset Timestamp, double Value, string Unit);

// Result is null when no sensor with this id exists at all; an empty list means the sensor
// exists but just doesn't have any readings yet — those are two different situations, and the
// endpoint maps them to different HTTP responses (404 vs. 200 with an empty array).
public record GetSensorHistoryQuery(Guid SensorId) : IRequest<IReadOnlyList<SensorReadingDto>?>;
