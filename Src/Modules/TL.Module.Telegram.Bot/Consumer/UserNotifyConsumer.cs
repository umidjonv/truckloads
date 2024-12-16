using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using RabbitMQ.Client;
using MediatR;
using RabbitMQ.Client.Events;
using TL.Shared.Common.Dtos.Telegram;
using TL.Shared.Core.MessageBroker;

namespace TL.Module.Telegram.Bot.Consumer;

public class UserNotifyConsumer(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<UserNotifyConsumer> logger,
    IHttpClientFactory httpClientFactory) : IUserNotifyConsumer
{
    public async Task Consume(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var rabbit = scope.ServiceProvider.GetRequiredService<IRabbitMqConnectionManager>();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var exchangeKey = configuration[$"{nameof(UserNotifyConsumer)}:ExchangeKey"];
        var routingKey = configuration[$"{nameof(UserNotifyConsumer)}:RoutingKey"];
        var queueKey = configuration[$"{nameof(UserNotifyConsumer)}:QueueKey"];

        if (string.IsNullOrWhiteSpace(exchangeKey))
        {
            logger.LogError(
                $"[{nameof(UserNotifyConsumer)}] {nameof(UserNotifyConsumer)}:ExchangeKey is empty!");
            return;
        }

        if (string.IsNullOrWhiteSpace(routingKey))
        {
            logger.LogError(
                $"[{nameof(UserNotifyConsumer)}] {nameof(UserNotifyConsumer)}:RoutingKey is empty!");
            return;
        }

        if (string.IsNullOrWhiteSpace(queueKey))
        {
            logger.LogError(
                $"[{nameof(UserNotifyConsumer)}] {nameof(UserNotifyConsumer)}:QueueKey is empty!");
            return;
        }

        var (_, channel) = await rabbit.GetConnection();

        await channel.ExchangeDeclareAsync(exchangeKey, ExchangeType.Direct,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(queueKey,
            false,
            false,
            false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(queueKey, exchangeKey, routingKey,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            try
            {
                var newPostParams = JsonSerializer.Deserialize<NewPostParams>(message);

                if (newPostParams != null) await NotifyAllUsersAsync(newPostParams, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception: {ex.Message}");
            }
        };

        await channel.BasicConsumeAsync(queueKey, true, consumer,
            cancellationToken);
    }

    private async Task NotifyAllUsersAsync(NewPostParams newPostParams, CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var users = await mediator.Send(new GetAllUserParams(), cancellationToken);

        //TODO:
        // Message ko'rinishini to'g'rilash kerak

        var message = $@"{newPostParams.StartLocation} - {newPostParams.DestinationLocation}
                        Yuk - {newPostParams.Cargo}
                        {newPostParams.TypeOfTruck}
                        {newPostParams.Weight}
                        Stavka - {newPostParams.Price}
                        {newPostParams.Type}
                        Narxi {newPostParams.Price}";
        foreach (var user in users)
        {
            var content = new StringContent($"{{\"chat_id\": \"{user.ChatId}\", \"text\": \"{message}\"}}",
                Encoding.UTF8, "application/json");

            await SendRequestAsync(content, cancellationToken);
        }
    }

    private async Task SendRequestAsync(StringContent content, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            using var client = httpClientFactory.CreateClient();

            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var botUrl = configuration["BotSettings:BotUrl"];
            ArgumentNullException.ThrowIfNull(botUrl);

            client.BaseAddress = new Uri(botUrl);

            var botToken = configuration["BotSettings:BotToken"];
            ArgumentNullException.ThrowIfNull(botToken);

            var sendMessagePath = configuration["BotSettings:SendMessagePath"];
            ArgumentNullException.ThrowIfNull(sendMessagePath);

            var url = $"/bot{botToken.TrimStart('/').TrimEnd('/')}/{sendMessagePath.TrimStart('/').TrimEnd('/')}";
            using var response =
                await client.PostAsync(url, content, cancellationToken);

            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            logger.LogError("[{0}] Error sending request: {1}", nameof(UserNotifyConsumer), ex.Message);
        }
    }
}