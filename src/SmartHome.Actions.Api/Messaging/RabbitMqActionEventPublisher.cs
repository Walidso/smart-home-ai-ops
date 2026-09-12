using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using SmartHome.Actions.Application.Actions;

namespace SmartHome.Actions.Api.Messaging;

// Opens a fresh connection per publish rather than holding one open — proposing an action is a
// human-triggered, low-frequency event, so the simplicity is worth more here than the
// connection-reuse efficiency would be. If RabbitMQ happens to be down, the exception is
// caught and logged rather than failing the whole request: the approval itself was already
// saved successfully, and that's the part that actually matters — Notify just won't get
// pinged for this one.
public class RabbitMqActionEventPublisher(ILogger<RabbitMqActionEventPublisher> logger) : IActionEventPublisher
{
    public const string QueueName = "actions.proposed";

    public async Task PublishActionProposedAsync(ActionProposedMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await channel.QueueDeclareAsync(
                queue: QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: QueueName, body: body, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not publish ActionProposed to RabbitMQ — the approval was still saved, but Notify won't be pinged for it.");
        }
    }
}
