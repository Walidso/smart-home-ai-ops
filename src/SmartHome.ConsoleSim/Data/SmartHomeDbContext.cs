using Microsoft.EntityFrameworkCore;
using SmartHome.Domain;

namespace SmartHome.ConsoleSim.Data;

public class SmartHomeDbContext : DbContext
{
    public DbSet<Sensor> Sensors => Set<Sensor>();
    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=smarthome.db");
    }
}
