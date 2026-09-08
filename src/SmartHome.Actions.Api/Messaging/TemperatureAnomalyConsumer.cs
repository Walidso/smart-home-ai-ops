using System.Text;
using System.Text.Json;
using MediatR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SmartHome.Actions.Application.Actions.ProposeAction;

namespace SmartHome.Actions.Api.Messaging;

// Matches the JSON shape SmartHome.ConsoleSim publishes — deliberately not a shared type
// between the two services, same reasoning as everywhere else this project avoids coupling
// service boundaries: the message schema is the contract, not a shared DLL.
public record TemperatureAnomalyEvent(string SensorName, string Room, double Value, string Unit, DateTimeOffset DetectedAt);

// Runs for the app's lifetime, listening for anomaly events published by the Sensors
// simulator and turning each one into a pending approval. Retries the RabbitMQ connection on
// a fixed delay instead of crashing the whole Api if the broker isn't up yet when this starts
// — services in a microservice system come up independently, so you can't assume your
// dependencies are ready the instant you are.
public class TemperatureAnomalyConsumer(IServiceScopeFactory scopeFactory, ILogger<TemperatureAnomalyConsumer> logger)
    : BackgroundService
{
    private const string QueueName = "sensors.temperature-anomaly";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IChannel? channel = await ConnectWithRetryAsync(stoppingToken);
        if (channel is null)
            return; // shutting down before we ever managed to connect

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) => await HandleMessageAsync(channel, ea);

        await channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        try
        {
            // BackgroundService expects ExecuteAsync to run until cancelled — this is what
            // keeps the subscription (and the channel/connection it's built on) alive.
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
    }

    private async Task<IChannel?> ConnectWithRetryAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory { HostName = "localhost" };
                var connection = await factory.CreateConnectionAsync(stoppingToken);
                var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
                await channel.QueueDeclareAsync(
                    queue: QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

                logger.LogInformation("Connected to RabbitMQ, listening for temperature anomalies.");
                return channel;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Could not connect to RabbitMQ ({Message}); retrying in 5s.", ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        return null;
    }

    private async Task HandleMessageAsync(IChannel channel, BasicDeliverEventArgs ea)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<TemperatureAnomalyEvent>(Encoding.UTF8.GetString(ea.Body.Span));
            if (evt is not null)
            {
                // BackgroundService itself is a singleton, but MediatR/DbContext are scoped —
                // so each message gets its own scope, same "short-lived unit of work" idea as
                // everywhere else in this project.
                using var scope = scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                await sender.Send(new ProposeActionCommand(
                    $"Turn off whatever's heating {evt.Room} — {evt.SensorName} hit {evt.Value}{evt.Unit}.",
                    evt.Room));

                logger.LogInformation("Auto-proposed an action for {SensorName} ({Value}{Unit}).", evt.SensorName, evt.Value, evt.Unit);
            }

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process a temperature anomaly message.");
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }
}
