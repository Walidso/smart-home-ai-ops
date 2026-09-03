using Microsoft.EntityFrameworkCore;
using SmartHome.Domain;
using SmartHome.Infrastructure.Data;

// --- Phase 4: apply migrations, then seed the house (only on the very first run) ----

List<Sensor> sensors;
using (var seedDb = new SmartHomeDbContext())
{
    seedDb.Database.Migrate();

    sensors = seedDb.Sensors.ToList();
    if (sensors.Count == 0)
    {
        sensors = new List<Sensor>
        {
            new() { Name = "Living Room Temp", Type = SensorType.Temperature, Room = "Living Room" },
            new() { Name = "Office Temp", Type = SensorType.Temperature, Room = "Office" },
            new() { Name = "Kitchen Humidity", Type = SensorType.Humidity, Room = "Kitchen" },
            new() { Name = "Main Panel Energy", Type = SensorType.EnergyUsage, Room = "Utility" },
            new() { Name = "Front Door", Type = SensorType.Door, Room = "Entrance" },
        };
        seedDb.Sensors.AddRange(sensors);
        seedDb.SaveChanges();
    }
}

// --- Phase 3: loop forever, generating a fresh batch of readings every few seconds ---

var random = new Random();

while (true)
{
    var readings = new List<SensorReading>();

    foreach (var sensor in sensors)
    {
        try
        {
            var (value, unit) = sensor.Type switch
            {
                SensorType.Temperature => (18 + random.NextDouble() * 12, "°C"),   // 18-30
                SensorType.Humidity => (30 + random.NextDouble() * 40, "%"),      // 30-70
                SensorType.EnergyUsage => (0.2 + random.NextDouble() * 3.5, "kW"), // 0.2-3.7
                SensorType.Door => (random.Next(0, 2), "open(1)/closed(0)"),
                SensorType.Motion => (random.Next(0, 2), "detected(1)/none(0)"),
                _ => (0.0, "unknown"),
            };

            readings.Add(new SensorReading
            {
                SensorId = sensor.Id,
                Timestamp = DateTimeOffset.UtcNow,
                Value = Math.Round(value, 1),
                Unit = unit,
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Skipping sensor '{sensor.Name}' — reading failed: {ex.Message}");
        }
    }

    // --- Phase 2: LINQ practice ---------------------------------------------------------
    // This is the part that maps straight to what you're learning right now.

    Console.WriteLine("=== SmartHome status report ===\n");

    // .Where() -> filter down to just the temperature readings
    var temperatureReadings = readings
        .Where(r => sensors.First(s => s.Id == r.SensorId).Type == SensorType.Temperature)
        .ToList();

    Console.WriteLine($"Temperature sensors reporting: {temperatureReadings.Count}");

    // .Select() -> project each reading into a friendly one-line description
    var summaries = readings.Select(r =>
    {
        var sensor = sensors.First(s => s.Id == r.SensorId);
        return $"- {sensor.Name}: {r.Value}{r.Unit}";
    });

    foreach (var line in summaries)
        Console.WriteLine(line);

    // .GroupBy() -> group readings by room, so we can talk about the house room-by-room
    Console.WriteLine("\n=== Grouped by room ===");
    var byRoom = readings
        .GroupBy(r => sensors.First(s => s.Id == r.SensorId).Room)
        .Select(g => new { Room = g.Key, Count = g.Count() });

    foreach (var group in byRoom)
        Console.WriteLine($"- {group.Room}: {group.Count} reading(s)");

    // .Any() -> the "is anything weird right now?" check.
    // Interview angle: Any() short-circuits and stops at the first match, which is why
    // it's better than Count() > 0 when you only care about *whether* something exists.
    bool anythingHot = readings.Any(r =>
        sensors.First(s => s.Id == r.SensorId).Type == SensorType.Temperature && r.Value > 27);

    Console.WriteLine($"\nAnything running hot (>27°C)? {(anythingHot ? "YES ⚠️" : "No, all good")}");

    // --- Phase 4: persist this batch, then prove it survives restarts by showing the
    // all-time total pulled back out of the database.
    using (var db = new SmartHomeDbContext())
    {
        db.SensorReadings.AddRange(readings);
        await db.SaveChangesAsync();

        var totalEver = await db.SensorReadings.CountAsync();
        Console.WriteLine($"Total readings ever recorded (survives restarts): {totalEver}");
    }

    // --- Phase 3: wait a few seconds before the next batch, without blocking the thread ---
    await Task.Delay(3000);
}

// --- What comes next -----------------------------------------------------------------
// Phase 4 saves readings to a real database with EF Core.
// Phase 7 is where this "anythingHot" check becomes something Hermes Agent can ask
// about and act on, through an MCP tool call. See ROADMAP.md.
