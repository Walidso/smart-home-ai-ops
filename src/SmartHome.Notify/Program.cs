using SmartHome.Notify;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient("ActionsApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ActionsApi:BaseUrl"] ?? "http://localhost:5073");
});

builder.Services.AddHostedService<ApprovalNotifierWorker>();

var host = builder.Build();
host.Run();
