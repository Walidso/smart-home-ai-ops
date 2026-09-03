using Microsoft.EntityFrameworkCore;
using SmartHome.Domain;

namespace SmartHome.Infrastructure.Data;

public class SmartHomeDbContext : DbContext
{
    public DbSet<Sensor> Sensors => Set<Sensor>();
    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();

    // Parameterless constructor: used when a plain console app does `new SmartHomeDbContext()`.
    public SmartHomeDbContext() { }

    // DI constructor: used by ASP.NET Core when the context is registered with AddDbContext.
    public SmartHomeDbContext(DbContextOptions<SmartHomeDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlite($"Data Source={GetDbPath()}");
    }

    // Every process that opens this database (the simulator, the API, whatever comes next)
    // needs to agree on exactly one file, regardless of its own working directory. A relative
    // path resolves differently depending on how/where an app happens to be launched, so
    // instead we walk up from wherever this code is running until we find the solution file,
    // and put the database right next to it.
    public static string GetDbPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && dir.GetFiles("SmartHomeAiOps.sln").Length == 0)
            dir = dir.Parent;

        var root = dir?.FullName ?? Directory.GetCurrentDirectory();
        return Path.Combine(root, "smarthome.db");
    }
}
