using SmartHome.McpGateway;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("SensorsApi", client =>
    client.BaseAddress = new Uri(builder.Configuration["SensorsApi:BaseUrl"] ?? "http://localhost:5152"));
builder.Services.AddHttpClient("ActionsApi", client =>
    client.BaseAddress = new Uri(builder.Configuration["ActionsApi:BaseUrl"] ?? "http://localhost:5073"));

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<HouseTools>();

var app = builder.Build();

app.MapMcp();

app.Run();
