using Microsoft.EntityFrameworkCore;
using SmartHome.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Register SmartHomeDbContext with the DI container. "Scoped" (AddDbContext's default) means
// one instance per incoming HTTP request — the same short-lived "unit of work" pattern from
// the console simulator, just handled automatically by the framework instead of a manual `using`.
builder.Services.AddDbContext<SmartHomeDbContext>(options =>
    options.UseSqlite($"Data Source={SmartHomeDbContext.GetDbPath()}"));

var app = builder.Build();

// Applying migrations here (once, at startup) is fine for a single app instance like this one.
// In a real multi-instance deployment you'd usually run migrations as a separate step, so two
// copies of the app don't race to migrate the same database at the same time.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>().Database.Migrate();
}

app.MapGet("/sensors", async (SmartHomeDbContext db) =>
    await db.Sensors
        .Select(s => new { s.Id, s.Name, s.Type, s.Room })
        .ToListAsync());

app.MapGet("/sensors/{id}/readings", async (Guid id, SmartHomeDbContext db) =>
{
    var sensorExists = await db.Sensors.AnyAsync(s => s.Id == id);
    if (!sensorExists)
        return Results.NotFound($"No sensor with id '{id}'.");

    // SQLite's EF Core provider can't translate ORDER BY on a DateTimeOffset column into SQL,
    // so we pull the rows first and sort them in memory instead of asking the database to.
    var readings = await db.SensorReadings
        .Where(r => r.SensorId == id)
        .Select(r => new { r.Id, r.Timestamp, r.Value, r.Unit })
        .ToListAsync();

    return Results.Ok(readings.OrderByDescending(r => r.Timestamp));
});

app.Run();
