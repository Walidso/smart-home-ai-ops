namespace SmartHome.Domain;

/// <summary>
/// One measurement from one sensor at one point in time. Think of this as a single
/// row you'd eventually store in a database (Phase 4) — for now it just lives in memory.
/// </summary>
public class SensorReading
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid SensorId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required double Value { get; init; }
    public required string Unit { get; init; }
}
