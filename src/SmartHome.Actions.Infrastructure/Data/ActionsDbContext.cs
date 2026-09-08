using Microsoft.EntityFrameworkCore;
using SmartHome.Actions.Domain;

namespace SmartHome.Actions.Infrastructure.Data;

public class ActionsDbContext : DbContext
{
    public DbSet<ProposedAction> ProposedActions => Set<ProposedAction>();

    public ActionsDbContext() { }

    public ActionsDbContext(DbContextOptions<ActionsDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlite($"Data Source={GetDbPath()}");
    }

    // Deliberately its own database file, separate from Sensors' smarthome.db — each
    // microservice owns its own schema and data, that's the whole point of splitting them
    // apart. See SmartHome.Infrastructure.Data.SmartHomeDbContext for the sibling version
    // of this same "find the solution root" trick.
    public static string GetDbPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && dir.GetFiles("SmartHomeAiOps.sln").Length == 0)
            dir = dir.Parent;

        var root = dir?.FullName ?? Directory.GetCurrentDirectory();
        return Path.Combine(root, "actions.db");
    }
}
