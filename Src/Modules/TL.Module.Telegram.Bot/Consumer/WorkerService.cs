using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TL.Shared.Core.MessageBroker;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TL.Module.Telegram.Bot.Consumer;

public interface IWorkerService
{
    Task ExecuteAsync(CancellationToken stoppingToken);
}

public class WorkerService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<WorkerService> logger,
    IHttpClientFactory factory) : IWorkerService
{
    private static readonly SemaphoreSlim Semaphore = new(Environment.ProcessorCount);

    public Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return StartProcessingQueue(stoppingToken);
    }

    private async Task StartProcessingQueue(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var rabbit = scope.ServiceProvider.GetRequiredService<IRabbitMqConnectionManager>();

        var configurationManager = scope.ServiceProvider.GetRequiredService<IConfigurationManager>();
        var exchangeKey = configurationManager[$"{nameof(WorkerService)}:ExchangeKey"];
        var routingKey = configurationManager[$"{nameof(WorkerService)}:RoutingKey"];
        var queueKey = configurationManager[$"{nameof(WorkerService)}:QueueKey"];

        if (string.IsNullOrWhiteSpace(exchangeKey))
        {
            logger.LogError($"[{nameof(WorkerService)}] {nameof(WorkerService)}:ExchangeKey is empty!");
            return;
        }

        if (string.IsNullOrWhiteSpace(routingKey))
        {
            logger.LogError($"[{nameof(WorkerService)}] {nameof(WorkerService)}:RoutingKey is empty!");
            return;
        }

        if (string.IsNullOrWhiteSpace(queueKey))
        {
            logger.LogError($"[{nameof(WorkerService)}] {nameof(WorkerService)}:QueueKey is empty!");
            return;
        }

        var (_, channel) = await rabbit.Connect();

        await channel.ExchangeDeclareAsync(exchange: exchangeKey, type: ExchangeType.Direct,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(queue: queueKey,
            durable: false,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(queue: queueKey, exchange: exchangeKey, routingKey: routingKey,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            try
            {
                var update = JsonSerializer.Deserialize<Update>(message);

                if (update != null)
                {
                    await ProcessUpdate(update, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception: {ex.Message}");
            }
        };

        await channel.BasicConsumeAsync(queue: queueKey, autoAck: true, consumer: consumer,
            cancellationToken: cancellationToken);
    }

    private async Task ProcessUpdate(Update update, CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        
        if (update.Message != null)
        {
            if (update.Type == UpdateType.Message && update.Message!.Type == MessageType.Text)
            {
                var message = update.Message.Text?.Trim() ?? string.Empty;
                var chatId = update.Message.Chat.Id;
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
    }

    private async Task HandleStartCommand(long chatId, CancellationToken cancellationToken)
    {
        string json = $@"
          {{
              ""chat_id"": ""{chatId}"",
              ""text"": ""Assalomu alaykum! Obuna bo'lish uchun tugmani bosing."",
              ""reply_markup"": {{
                  ""inline_keyboard"": [
                      [
                          {{ ""text"": ""Obuna bo'lish"", ""callback_data"": ""subscribe"" }}
                      ]
                  ]
              }}
          }}";

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        await SendRequestAsync(content, cancellationToken);
    }

    private async Task HandleHelpCommand(long chatId, CancellationToken cancellationToken)
    {
        string helpMessage =
            "Salom! Bu botni ishlatish uchun obuna bo'lishingiz kerak. Quyidagi komandalardan foydalanishingiz mumkin:\n\n" +
            "/start - Botni ishga tushurish\n" +
            "/help - Yordam olish\n" +
            "Obuna bo'lish uchun: 'Obuna bo'lish' tugmasini bosing.";

        var content = new StringContent($"{{\"chat_id\": \"{chatId}\", \"text\": \"{helpMessage}\"}}", Encoding.UTF8,
            "application/json");

        await SendRequestAsync(content, cancellationToken);
    }

    private async Task SendRequestAsync(StringContent content, CancellationToken cancellationToken)
    {
        await Semaphore.WaitAsync(cancellationToken);

        try
        {
            using var client = factory.CreateClient();

            using var response =
                await client.PostAsync(StaticData.BotBase_Url, content, cancellationToken);

            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            logger.LogError("[{0}] Error sending request: {1}", nameof(WorkerService), ex.Message);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private async Task HandleCallbackQuery(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        string? callbackData = callbackQuery.Data;
        long userId = callbackQuery.From.Id;
        string userName = callbackQuery.From.Username ?? "NoUsername";
        long chatId = callbackQuery.Message.Chat.Id;
        string message = $"Rahmat, @{userName}! Siz obuna bo'ldingiz.";

        if (callbackData == "subscribe")
        {
            var content = new StringContent(
                $"{{\"chat_id\": \"{chatId}\", \"text\": \"{message}\"}}",
                Encoding.UTF8,
                "application/json");

            await SendRequestAsync(content, cancellationToken);

            Console.WriteLine($"Obuna bo'lgan user ID: {userId}, Username: @{userName}");

            /*// UserDto ma'lumotlarini bazaga saqlash
            UserDto userDto = new UserDto()
            {
                UserId = userId,
                UserName = userName ?? "",
                FirstName = callbackQuery.From.FirstName ?? "",
                LastName = callbackQuery.From.LastName ?? ""

            };*/

            // Bazaga qo'shish (Bu yerda konkret kodni yozing)
            //await SaveUserToDatabase(userDto);
        }
    }

    private async Task HandleDefaultCommand(long chatId, CancellationToken cancellationToken)
    {
        string unknownCommandMessage = "Kechirasiz, bu buyruqni tushunmadim. Quyidagi komandalardan foydalaning:\n\n" +
                                       "/start - Botni ishga tushurish\n" +
                                       "/help - Yordam olish\n" +
                                       "Obuna bo'lish uchun: 'Obuna bo'lish' tugmasini bosing.";

        var content = new StringContent($"{{\"chat_id\": \"{chatId}\", \"text\": \"{unknownCommandMessage}\"}}",
            Encoding.UTF8, "application/json");

        await SendRequestAsync(content, cancellationToken);
    }
}