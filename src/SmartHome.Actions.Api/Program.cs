using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartHome.Actions.Application.Actions;
using SmartHome.Actions.Application.Actions.ApproveAction;
using SmartHome.Actions.Application.Actions.ListPendingApprovals;
using SmartHome.Actions.Application.Actions.ProposeAction;
using SmartHome.Actions.Application.Actions.RejectAction;
using SmartHome.Actions.Application.Behaviors;
using SmartHome.Actions.Infrastructure.Data;
using SmartHome.Actions.Api.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Same reasoning as the Sensors Api: pin FluentValidation's messages to English regardless of
// the host machine's OS culture.
ValidatorOptions.Global.LanguageManager.Enabled = false;

builder.Services.AddDbContext<ActionsDbContext>(options =>
    options.UseSqlite($"Data Source={ActionsDbContext.GetDbPath()}"));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ProposeActionCommand).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(ProposeActionCommand).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddHostedService<TemperatureAnomalyConsumer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ActionsDbContext>().Database.Migrate();
}

app.MapPost("/actions", async (ProposeActionCommand command, ISender sender) =>
{
    try
    {
        var action = await sender.Send(command);
        return Results.Created($"/actions/{action.Id}", action);
    }
    catch (ValidationException ex)
    {
        return ex.ToValidationProblem();
    }
});

app.MapGet("/actions/pending", async (ISender sender) =>
    Results.Ok(await sender.Send(new ListPendingApprovalsQuery())));

app.MapPost("/actions/{id}/approve", async (Guid id, ISender sender) =>
{
    try
    {
        var outcome = await sender.Send(new ApproveActionCommand(id));
        return outcome.ToHttpResult(id);
    }
    catch (ValidationException ex)
    {
        return ex.ToValidationProblem();
    }
});

app.MapPost("/actions/{id}/reject", async (Guid id, ISender sender) =>
{
    try
    {
        var outcome = await sender.Send(new RejectActionCommand(id));
        return outcome.ToHttpResult(id);
    }
    catch (ValidationException ex)
    {
        return ex.ToValidationProblem();
    }
});

app.Run();

static class ResultMappingExtensions
{
    public static IResult ToValidationProblem(this ValidationException ex) =>
        Results.ValidationProblem(ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

    public static IResult ToHttpResult(this DecisionOutcome outcome, Guid actionId) => outcome switch
    {
        DecisionOutcome.Success => Results.NoContent(),
        DecisionOutcome.NotFound => Results.NotFound($"No proposed action with id '{actionId}'."),
        DecisionOutcome.AlreadyDecided => Results.Conflict($"Action '{actionId}' has already been decided."),
        _ => throw new InvalidOperationException($"Unhandled {nameof(DecisionOutcome)}: {outcome}"),
    };
}
