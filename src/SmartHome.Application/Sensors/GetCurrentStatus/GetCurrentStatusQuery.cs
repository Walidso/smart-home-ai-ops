using MediatR;
using SmartHome.Domain;

namespace SmartHome.Application.Sensors.GetCurrentStatus;

public record SensorStatus(
    Guid SensorId,
    string SensorName,
    string Room,
    SensorType Type,
    double? LatestValue,
    string? Unit,
    DateTimeOffset? LastReadingAt);

public record CurrentStatusResult(IReadOnlyList<SensorStatus> Sensors, bool AnythingHot);

// No parameters, so there's nothing for FluentValidation to check here — not every
// command/query needs a validator, only ones with something meaningful to validate.
public record GetCurrentStatusQuery : IRequest<CurrentStatusResult>;
