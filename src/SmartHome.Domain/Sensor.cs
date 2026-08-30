namespace SmartHome.Domain;

/// <summary>
/// A single sensor in the house. This is a plain C# class with no database or web
/// code in it at all — that's on purpose. In Clean Architecture terms this lives in
/// the "Domain" layer: the pure idea of what a Sensor *is*, independent of how it's
/// stored or exposed later.
/// </summary>
public class Sensor
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required SensorType Type { get; init; }
    public required string Room { get; init; }

    // A sensor knows how to describe itself in plain language — handy for the
    // console output now, and later for whatever Hermes ends up saying out loud.
    public override string ToString() => $"{Name} ({Type}) in {Room}";
}
