using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartHome.Application.Behaviors;
using SmartHome.Application.Sensors.GetCurrentStatus;
using SmartHome.Application.Sensors.GetSensorHistory;
using SmartHome.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// FluentValidation auto-localizes its default error messages based on the server's OS
// culture (this machine's happened to produce Swedish!). An API's error messages shouldn't
// depend on which machine happens to be hosting it, so pin them to English.
ValidatorOptions.Global.LanguageManager.Enabled = false;

// Register SmartHomeDbContext with the DI container. "Scoped" (AddDbContext's default) means
// one instance per incoming HTTP request — the same short-lived "unit of work" pattern from
// the console simulator, just handled automatically by the framework instead of a manual `using`.
builder.Services.AddDbContext<SmartHomeDbContext>(options =>
    options.UseSqlite($"Data Source={SmartHomeDbContext.GetDbPath()}"));

// MediatR scans the Application assembly for IRequestHandler<,> implementations and wires them
// up automatically — Send(new SomeQuery()) finds the matching handler without any manual mapping.
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetSensorHistoryQuery).Assembly));

// Same idea for FluentValidation: scans for AbstractValidator<T> implementations and registers
// each one, so ValidationBehavior can find "the validator for this request type", if any.
builder.Services.AddValidatorsFromAssembly(typeof(GetSensorHistoryQuery).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

var app = builder.Build();

// Applying migrations here (once, at startup) is fine for a single app instance like this one.
// In a real multi-instance deployment you'd usually run migrations as a separate step, so two
// copies of the app don't race to migrate the same database at the same time.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>().Database.Migrate();
}

// Left as a direct DbContext call — simple enough that wrapping it in a query wouldn't add
// anything. GetSensorHistoryQuery and GetCurrentStatusQuery are the ones with actual logic
// worth pulling into the Application layer.
app.MapGet("/sensors", async (SmartHomeDbContext db) =>
    await db.Sensors
        .Select(s => new { s.Id, s.Name, s.Type, s.Room })
        .ToListAsync());

app.MapGet("/status", async (ISender sender) =>
    Results.Ok(await sender.Send(new GetCurrentStatusQuery())));

app.MapGet("/sensors/{id}/readings", async (Guid id, ISender sender) =>
{
    try
    {
        var readings = await sender.Send(new GetSensorHistoryQuery(id));
        return readings is null
            ? Results.NotFound($"No sensor with id '{id}'.")
            : Results.Ok(readings);
    }
    catch (ValidationException ex)
    {
        return Results.ValidationProblem(ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
    }
});

app.Run();
