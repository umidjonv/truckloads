using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TL.Shared.Core.MessageBroker;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TL.Shared.Common.Dtos.Telegram;

namespace TL.Module.Telegram.Bot.Consumer;

public class TelegramBotCommandConsumer(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<TelegramBotCommandConsumer> logger,
    IHttpClientFactory httpClientFactory) : ITelegramBotCommandConsumer
{
    private static readonly ConcurrentDictionary<long, SemaphoreSlim> ConcurrentControl = [];

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var rabbit = scope.ServiceProvider.GetRequiredService<IRabbitMqConnectionManager>();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var exchangeKey = configuration[$"{nameof(TelegramBotUpdateConsumer)}:ExchangeKey"];
        var routingKey = configuration[$"{nameof(TelegramBotUpdateConsumer)}:RoutingKey"];
        var queueKey = configuration[$"{nameof(TelegramBotUpdateConsumer)}:QueueKey"];
        

        if (string.IsNullOrWhiteSpace(exchangeKey))
        {
            logger.LogError(
                $"[{nameof(TelegramBotCommandConsumer)}] {nameof(TelegramBotUpdateConsumer)}:ExchangeKey is empty!");
            return;
        }

        if (string.IsNullOrWhiteSpace(routingKey))
        {
            logger.LogError(
                $"[{nameof(TelegramBotCommandConsumer)}] {nameof(TelegramBotUpdateConsumer)}:RoutingKey is empty!");
            return;
        }

        if (string.IsNullOrWhiteSpace(queueKey))
        {
            logger.LogError(
                $"[{nameof(TelegramBotCommandConsumer)}] {nameof(TelegramBotUpdateConsumer)}:QueueKey is empty!");
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
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            try
            {
                var update = JsonSerializer.Deserialize<Update>(message);

                if (update != null) await ProcessUpdate(update, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception: {ex.Message}");
            }
        };

        await channel.BasicConsumeAsync(queueKey, true, consumer,
            cancellationToken);
    }

    private async Task ProcessUpdate(Update update, CancellationToken cancellationToken)
    {
        if (update.Message != null)
        {
            var chatId = update.Message.Chat.Id;
            var semaphore = ConcurrentControl.GetOrAdd(chatId, _ => new SemaphoreSlim(1));
            try
            {
                await semaphore.WaitAsync(cancellationToken);

                if (update.Type == UpdateType.Message && update.Message!.Type == MessageType.Text)
                {
                    var message = update.Message.Text?.Trim() ?? string.Empty;
                    switch (message)
                    {
                        case "/start":
                        {
                            await HandleStartCommand(chatId, cancellationToken);
                            break;
                        }
                        case "/help":
                        {
                            await HandleHelpCommand(chatId, cancellationToken);
                            break;
                        }
                        default:
                        {
                            await HandleDefaultCommand(chatId, cancellationToken);
                            break;
                        }
                    }
                }
                else if (update.Type == UpdateType.CallbackQuery)
                {
                    await HandleCallbackQuery(update.CallbackQuery, cancellationToken);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception while getting message");
            }
            finally
            {
                ConcurrentControl.TryRemove(chatId, out _);
                semaphore.Release();
            }
        }
    }

    private async Task HandleStartCommand(long chatId, CancellationToken cancellationToken)
    {
        var json = $@"
                 {{
                     ""chat_id"": ""{chatId}"",
                     ""text"": ""Hello! You need to subscribe to use this bot. Please click the button to subscribe."",
                     ""reply_markup"": {{
                         ""inline_keyboard"": [
                             [
                                 {{ ""text"": ""Subscribe"", ""callback_data"": ""subscribe"" }}
                                          ]
                     }}
                 }}";

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        await SendRequestAsync(content, cancellationToken);
    }

    private async Task HandleHelpCommand(long chatId, CancellationToken cancellationToken)
    {
        var helpMessage =
            "Hello! You need to subscribe to use this bot. You can use the following commands:\n:\n\n" +
            "/start - Start the bot\n" +
            "/help - Get help\n" +
            "To subscribe: Click the 'Subscribe' button.\n";

        var content = new StringContent($"{{\"chat_id\": \"{chatId}\", \"text\": \"{helpMessage}\"}}", Encoding.UTF8,
            "application/json");

        await SendRequestAsync(content, cancellationToken);
    }
   
    private async Task HandleCallbackQuery(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var callbackData = callbackQuery.Data;
        var userId = callbackQuery.From.Id;
        var userName = callbackQuery.From.Username ?? "NoUsername";
        var chatId = callbackQuery.Message?.Chat.Id;
        var message = $"Thank you!, @{userName}! You have subscribed.";

        if (callbackData == "subscribe" && chatId.HasValue)
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            var insertUserParams = new InsertUserParams(chatId.Value, userId, userName);
            var res= await mediator.Send(insertUserParams, cancellationToken);
            if (res)
            {
                var content = new StringContent(
                    $"{{\"chat_id\": \"{chatId}\", \"text\": \"{message}\"}}",
                    Encoding.UTF8,
                    "application/json");

                await SendRequestAsync(content, cancellationToken);
            }
            else
            {
                logger.LogError("Failed to save user information to the database.");
            }
        }
    }

    private async Task HandleDefaultCommand(long chatId, CancellationToken cancellationToken)
    {
        var unknownCommandMessage = "Sorry, I didn't understand this command. Please use one of the following commands:\n\n" +
                                    "/start - Start the bot\n" +
                                    "/help - Get help\n" +
                                    "To subscribe: Click the 'Subscribe' button.";

        var content = new StringContent($"{{\"chat_id\": \"{chatId}\", \"text\": \"{unknownCommandMessage}\"}}",
            Encoding.UTF8, "application/json");

        await SendRequestAsync(content, cancellationToken);
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
            logger.LogError("[{0}] Error sending request: {1}", nameof(TelegramBotCommandConsumer), ex.Message);
        }
    }

}