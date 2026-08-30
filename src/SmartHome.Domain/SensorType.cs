namespace SmartHome.Domain;

/// <summary>
/// The kinds of sensors our simulated house has. Add more here as the house grows
/// (e.g. Smoke, Water, Light) — this is a great "safe" place to practice adding
/// small features without breaking anything else.
/// </summary>
public enum SensorType
{
    Temperature,
    Humidity,
    EnergyUsage,
    Motion,
    Door
}
