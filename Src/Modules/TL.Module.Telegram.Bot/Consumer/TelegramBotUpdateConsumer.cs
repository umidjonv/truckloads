using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TL.Shared.Core.MessageBroker;

namespace TL.Module.Telegram.Bot.Consumer;

public interface ITelegramBotUpdateConsumer : IUpdateHandler
{
    Task StartReceiving(CancellationToken cancellationToken = default);
}

public class TelegramBotUpdateConsumer(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<TelegramBotUpdateConsumer> logger) : ITelegramBotUpdateConsumer
{
    public async Task StartReceiving(CancellationToken cancellationToken = default)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var token = configuration["BotSettings:BotToken"];
        if (string.IsNullOrWhiteSpace(token))
            ArgumentNullException.ThrowIfNull(token);

        var client = new TelegramBotClient(token);
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Enum.GetValues<UpdateType>()
        };

        client.StartReceiving(updateHandler: this, receiverOptions: receiverOptions,
            cancellationToken: cancellationToken);
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message!.Type == MessageType.Text)
            await SendMessageToQueue(update, cancellationToken);
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        logger.LogError($"Xatolik bor {exception}");
        return Task.CompletedTask;
    }

    private async Task SendMessageToQueue(Update update, CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        
        var exchangeKey = configuration[$"{nameof(TelegramBotUpdateConsumer)}:ExchangeKey"];
        var routingKey = configuration[$"{nameof(TelegramBotUpdateConsumer)}:RoutingKey"];
        var queueKey = configuration[$"{nameof(TelegramBotUpdateConsumer)}:QueueKey"];
        
        if (string.IsNullOrWhiteSpace(exchangeKey))
        {
            logger.LogError(
                $"[{nameof(TelegramBotCommandConsumer)}] {nameof(TelegramBotCommandConsumer)}:ExchangeKey is empty!");
            return;
        }

        if (string.IsNullOrWhiteSpace(routingKey))
        {
            logger.LogError(
                $"[{nameof(TelegramBotCommandConsumer)}] {nameof(TelegramBotCommandConsumer)}:RoutingKey is empty!");
            return;
        }

        if (string.IsNullOrWhiteSpace(queueKey))
        {
            logger.LogError(
                $"[{nameof(TelegramBotCommandConsumer)}] {nameof(TelegramBotCommandConsumer)}:QueueKey is empty!");
            return;
        }
        
        var rabbit = scope.ServiceProvider.GetRequiredService<IRabbitMqConnectionManager>();

        await rabbit.PublishAsync(exchangeKey, routingKey, queueKey, update, cancellationToken);

        logger.LogInformation("Update ma'lumotlari muvaffaqiyatli yuborildi: {UpdateId}", update.Id);
    }
}