using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TL.Shared.Common.Dtos.Telegram;
using TL.Shared.Core.MessageBroker;

namespace TL.Module.Telegram.Worker.Consumers;

public class InsertMessageConsumer(IServiceScopeFactory serviceScopeFactory) : IInsertMessageConsumer
{
    public async Task Consume(CancellationToken cancellationToken = default)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<InsertMessageConsumer>>();
        var rabbit = scope.ServiceProvider.GetRequiredService<IRabbitMqConnectionManager>();

        var (_, channel) = await rabbit.GetConnection();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var exchangeKey = configuration[$"{nameof(InsertMessageConsumer)}:ExchangeKey"];
        var routingKey = configuration[$"{nameof(InsertMessageConsumer)}:RoutingKey"];
        var queueKey = configuration[$"{nameof(InsertMessageConsumer)}:QueueKey"];

        if (string.IsNullOrWhiteSpace(exchangeKey))
        {
            logger.LogError($"[{nameof(InsertMessageConsumer)}] {nameof(InsertMessageConsumer)}:ExchangeKey is empty!");
            return;
        }

        if (string.IsNullOrWhiteSpace(routingKey))
        {
            logger.LogError($"[{nameof(InsertMessageConsumer)}] {nameof(InsertMessageConsumer)}:RoutingKey is empty!");
            return;
        }

        if (string.IsNullOrWhiteSpace(queueKey))
        {
            logger.LogError($"[{nameof(InsertMessageConsumer)}] {nameof(InsertMessageConsumer)}:QueueKey is empty!");
            return;
        }

        var dlxExchange = $"{exchangeKey}.dlx";
        var dlxQueueKey = $"{queueKey}.dead-letter";

        await channel.ExchangeDeclareAsync(dlxExchange, ExchangeType.Direct,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(dlxQueueKey,
            true,
            false,
            false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(dlxQueueKey, dlxExchange, dlxQueueKey,
            cancellationToken: cancellationToken);

        var queueArguments = new Dictionary<string, object?>
        {
            { "x-dead-letter-exchange", dlxExchange },
            { "x-dead-letter-routing-key", dlxQueueKey }
        };
        
        await channel.ExchangeDeclareAsync(exchangeKey, ExchangeType.Direct,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(queueKey,
            true,
            false,
            false,
            queueArguments,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(queueKey, exchangeKey, routingKey,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) => await Receive(channel, ea, cancellationToken);

        await channel.BasicConsumeAsync(queueKey,
            false,
            consumer, cancellationToken);

        await channel.BasicQosAsync(0, (ushort)Environment.ProcessorCount, false, cancellationToken);
    }

    private async Task Receive(IChannel channel, BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var body = Encoding.UTF8.GetString(ea.Body.ToArray());
        var message = JsonSerializer.Deserialize<InsertMessageParams>(body);

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<InsertMessageConsumer>>();

        try
        {
            if (message is not null)
            {
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var exchangeKey = configuration[$"ConvertMessageToJsonConsumer:ExchangeKey"];
                var routingKey = configuration[$"ConvertMessageToJsonConsumer:RoutingKey"];
                var queueKey = configuration[$"ConvertMessageToJsonConsumer:QueueKey"];

                if (string.IsNullOrWhiteSpace(exchangeKey))
                {
                    logger.LogError(
                        $"[{nameof(InsertMessageConsumer)}] ConvertMessageToJsonConsumer:ExchangeKey is empty!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(routingKey))
                {
                    logger.LogError(
                        $"[{nameof(InsertMessageConsumer)}] ConvertMessageToJsonConsumer:RoutingKey is empty!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(queueKey))
                {
                    logger.LogError(
                        $"[{nameof(InsertMessageConsumer)}] ConvertMessageToJsonConsumer:QueueKey is empty!");
                    return;
                }

                var rabbit = scope.ServiceProvider.GetRequiredService<IRabbitMqConnectionManager>();

                await rabbit.PublishAsync(exchangeKey, routingKey, queueKey, message.Message, cancellationToken);

                await mediator.Send(message, cancellationToken);

                await channel.BasicAckAsync(ea.DeliveryTag, false,
                    cancellationToken);
            }
        }
        catch (Exception e)
        {
            logger.LogError("[{0}] Insert message failed. Sending to DLX. Details: {1}",
                nameof(InsertMessageConsumer), e.Message);

            await channel.BasicNackAsync(ea.DeliveryTag, false, false,
                cancellationToken);
        }
    }
}