using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace SmartHome.ConsoleSim.Messaging;

public record TemperatureAnomalyEvent(string SensorName, string Room, double Value, string Unit, DateTimeOffset DetectedAt);

// Publishes to RabbitMQ so the Actions service can react without Sensors knowing or caring
// whether anything is even listening. If RabbitMQ isn't reachable, the simulator keeps
// running anyway — its core job (generating/printing/persisting readings) shouldn't depend
// on the message bus being up.
public sealed class TemperatureAnomalyPublisher : IAsyncDisposable
{
    public const string QueueName = "sensors.temperature-anomaly";

    private readonly IConnection _connection;
    private readonly IChannel _channel;

    private TemperatureAnomalyPublisher(IConnection connection, IChannel channel)
    {
        _connection = connection;
        _channel = channel;
    }

    public static async Task<TemperatureAnomalyPublisher?> TryCreateAsync()
    {
        try
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            var connection = await factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();
            await channel.QueueDeclareAsync(queue: QueueName, durable: true, exclusive: false, autoDelete: false);

            Console.WriteLine("[EVENT] Connected to RabbitMQ — will publish temperature anomalies.");
            return new TemperatureAnomalyPublisher(connection, channel);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Could not connect to RabbitMQ — anomaly events won't be published this run: {ex.Message}");
            return null;
        }
    }

    public async Task PublishAsync(TemperatureAnomalyEvent evt)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));
        await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: QueueName, body: body);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }
}
