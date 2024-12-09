using System.Text;
using System.Text.Json;
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

public class WorkerService(IServiceScopeFactory serviceScopeFactory, ILogger<WorkerService> logger) : BackgroundService
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(10);
    
    
  public async Task StartProcessingQueue(CancellationToken cancellationToken)
  {
      await using var scope = serviceScopeFactory.CreateAsyncScope();
      var rabbit = scope.ServiceProvider.GetRequiredService<IRabbitMqConnectionManager>();
      
      var (_, channel) = await rabbit.Connect();
  
      
      await channel.QueueDeclareAsync(queue: "updates",
          durable: false,
          exclusive: false,
          autoDelete: false,
          arguments: null,
          cancellationToken: cancellationToken);
      
      var consumer = new AsyncEventingBasicConsumer(channel);
      
      
      consumer.ReceivedAsync += async (model, ea) =>
      {
          var body = ea.Body.ToArray();
          var message = Encoding.UTF8.GetString(body);
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
  
          logger.LogInformation($"Received message: {message}");
      };
  
      await channel.BasicConsumeAsync(queue: "updates", autoAck: true, consumer: consumer);
  }



    private async Task ProcessUpdate(Update update, CancellationToken cancellationToken)
    {
       await using var scope = serviceScopeFactory.CreateAsyncScope();
       var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

       if (update.Message != null)
       {
           if (update.Type == UpdateType.Message && update.Message!.Type == MessageType.Text)
           { 
               string message = update.Message.Text?.Trim() ?? string.Empty;
               long chatId = update.Message.Chat.Id;
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
                       await HandleDefultCommand(chatId, cancellationToken);
                       break;
                   }
                  
               }
           }
           else if(update.Type == UpdateType.CallbackQuery)
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
        string helpMessage = "Salom! Bu botni ishlatish uchun obuna bo'lishingiz kerak. Quyidagi komandalardan foydalanishingiz mumkin:\n\n" +
                             "/start - Botni ishga tushurish\n" +
                             "/help - Yordam olish\n" +
                             "Obuna bo'lish uchun: 'Obuna bo'lish' tugmasini bosing.";

        var content = new StringContent($"{{\"chat_id\": \"{chatId}\", \"text\": \"{helpMessage}\"}}", Encoding.UTF8, "application/json");

        await SendRequestAsync(content, cancellationToken);
    }
    
    private async Task SendRequestAsync(StringContent content, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken); // Limit concurrent requests

        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(StaticData.BotBase_Url, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Xatolik yuz berdi: {response.StatusCode}\n{error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending request: {ex.Message}");
        }
        finally
        {
            _semaphore.Release(); 
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
    private async Task HandleDefultCommand(long chatId, CancellationToken cancellationToken)
    {
        string unknownCommandMessage = "Kechirasiz, bu buyruqni tushunmadim. Quyidagi komandalardan foydalaning:\n\n" +
                                       "/start - Botni ishga tushurish\n" +
                                       "/help - Yordam olish\n" +
                                       "Obuna bo'lish uchun: 'Obuna bo'lish' tugmasini bosing.";

        var content = new StringContent($"{{\"chat_id\": \"{chatId}\", \"text\": \"{unknownCommandMessage}\"}}", Encoding.UTF8, "application/json");

        await SendRequestAsync(content, cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await StartProcessingQueue(stoppingToken);
    }
}