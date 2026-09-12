using System.Net.Http;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SmartHome.Notify;

// Two jobs in one worker: (1) listen on RabbitMQ for ActionProposed events and ping a Telegram
// chat about each one with Approve/Reject buttons, and (2) listen for button presses on that
// bot and call Actions.Api's approve/reject endpoints. Kept as one class since both halves
// share the same TelegramBotClient and there's no benefit to splitting them across services
// that would then need to coordinate.
public class ApprovalNotifierWorker(
    IConfiguration config,
    IHttpClientFactory httpClientFactory,
    ILogger<ApprovalNotifierWorker> logger) : BackgroundService
{
    private const string QueueName = "actions.proposed";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var botToken = config["Telegram:BotToken"]
            ?? throw new InvalidOperationException("Telegram:BotToken is not configured. Set it with `dotnet user-secrets set \"Telegram:BotToken\" \"...\"`.");
        var chatId = config["Telegram:ChatId"]
            ?? throw new InvalidOperationException("Telegram:ChatId is not configured. Set it with `dotnet user-secrets set \"Telegram:ChatId\" \"...\"`.");

        var bot = new TelegramBotClient(botToken);

        // Long polling: the bot repeatedly asks Telegram "any updates for me?" rather than
        // needing a public HTTPS endpoint for Telegram to call — much simpler for local dev.
        bot.StartReceiving(
            updateHandler: (client, update, ct) => HandleTelegramUpdateAsync(client, update, chatId, ct),
            errorHandler: HandleTelegramErrorAsync,
            receiverOptions: new ReceiverOptions { AllowedUpdates = [UpdateType.CallbackQuery] },
            cancellationToken: stoppingToken);

        logger.LogInformation("Telegram bot receiving updates. Listening on RabbitMQ for proposed actions...");

        var channel = await ConnectWithRetryAsync(stoppingToken);
        if (channel is null)
            return; // shutting down before we ever managed to connect

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) => await HandleActionProposedAsync(channel, ea, bot, chatId);

        await channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        try
        {
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

    private async Task HandleActionProposedAsync(IChannel channel, BasicDeliverEventArgs ea, ITelegramBotClient bot, string chatId)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<ActionProposedMessage>(Encoding.UTF8.GetString(ea.Body.Span));
            if (evt is not null)
            {
                var keyboard = new InlineKeyboardMarkup(
                [
                    [
                        InlineKeyboardButton.WithCallbackData("✅ Approve", $"approve:{evt.Id}"),
                        InlineKeyboardButton.WithCallbackData("❌ Reject", $"reject:{evt.Id}"),
                    ],
                ]);

                var room = evt.TargetRoom is null ? "" : $" ({evt.TargetRoom})";
                await bot.SendMessage(
                    chatId: chatId,
                    text: $"🏠 New proposed action{room}:\n{evt.Description}",
                    replyMarkup: keyboard);

                logger.LogInformation("Sent Telegram approval request for action {ActionId}.", evt.Id);
            }

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process an ActionProposed message.");
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }

    private async Task HandleTelegramUpdateAsync(ITelegramBotClient bot, Update update, string chatId, CancellationToken ct)
    {
        var callback = update.CallbackQuery;
        if (callback?.Data is null)
            return;

        var parts = callback.Data.Split(':', 2);
        if (parts.Length != 2 || !Guid.TryParse(parts[1], out var actionId))
            return;

        var decision = parts[0]; // "approve" or "reject"
        var client = httpClientFactory.CreateClient("ActionsApi");

        try
        {
            var response = await client.PostAsync($"/actions/{actionId}/{decision}", content: null, ct);

            var resultText = response.StatusCode switch
            {
                System.Net.HttpStatusCode.NoContent => $"{(decision == "approve" ? "✅ Approved" : "❌ Rejected")}.",
                System.Net.HttpStatusCode.Conflict => "⚠️ Already decided by someone else.",
                System.Net.HttpStatusCode.NotFound => "⚠️ Action no longer exists.",
                _ => $"⚠️ Unexpected response: {response.StatusCode}",
            };

            await bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
            await bot.EditMessageText(
                chatId: callback.Message!.Chat.Id,
                messageId: callback.Message.MessageId,
                text: $"{callback.Message.Text}\n\n{resultText}",
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to relay a Telegram decision to Actions.Api.");
            await bot.AnswerCallbackQuery(callback.Id, "Something went wrong — check the logs.", cancellationToken: ct);
        }
    }

    private Task HandleTelegramErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken ct)
    {
        logger.LogError(exception, "Telegram polling error.");
        return Task.CompletedTask;
    }
}
