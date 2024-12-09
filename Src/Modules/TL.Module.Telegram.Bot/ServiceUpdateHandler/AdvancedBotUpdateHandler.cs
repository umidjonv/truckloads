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

namespace TL.Module.Telegram.Bot.ServiceUpdateHandler;

public class AdvancedBotUpdateHandler(IServiceScopeFactory serviceScopeFactory,ILogger<AdvancedBotUpdateHandler> logger) : IUpdateHandler
{

    
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message!.Type == MessageType.Text)
        {
            await SendMessageToQueue(update,cancellationToken: cancellationToken);
        }
        

    }

    public  Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        logger.LogError($"Xatolik bor {exception}");
        return Task.CompletedTask;
    }



    public async Task SendMessageToQueue(Update update,CancellationToken cancellationToken)
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

        var messageBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(update));
        var properties = new BasicProperties
        {
            Persistent = true
        };
        
        cancellationToken.ThrowIfCancellationRequested();
        
        await channel.BasicPublishAsync(
            exchange: string.Empty, 
            routingKey: "updates",
            mandatory: true,
            basicProperties: properties,
            body: messageBody,
            cancellationToken: cancellationToken);

        logger.LogInformation("Update ma'lumotlari muvaffaqiyatli yuborildi: {UpdateId}", update.Id); 

    }
}

 