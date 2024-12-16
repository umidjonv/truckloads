using System.Text;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TL.Shared.Core.MessageBroker;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TL.Shared.Common.Dtos.AIProcessing;
using TL.Shared.Core.Mongo;

namespace TL.Module.AIProcessing.Worker.Consumers;

public class ConvertMessageToJsonConsumer(IServiceScopeFactory serviceScopeFactory) : IConvertMessageToJsonConsumer
{
    public async Task Consume(CancellationToken cancellationToken = default)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ConvertMessageToJsonConsumer>>();
        var rabbit = scope.ServiceProvider.GetRequiredService<IRabbitMqConnectionManager>();

        var (_, channel) = await rabbit.GetConnection();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var exchangeKey = configuration[$"{nameof(ConvertMessageToJsonConsumer)}:ExchangeKey"];
        var routingKey = configuration[$"{nameof(ConvertMessageToJsonConsumer)}:RoutingKey"];
        var queueKey = configuration[$"{nameof(ConvertMessageToJsonConsumer)}:QueueKey"];

        if (string.IsNullOrWhiteSpace(exchangeKey))
        {
            logger.LogError(
                $"[{nameof(ConvertMessageToJsonConsumer)}] ConvertMessageToJsonConsumer:ExchangeKey is empty!");
            return;
        }

        if (string.IsNullOrWhiteSpace(routingKey))
        {
            logger.LogError(
                $"[{nameof(ConvertMessageToJsonConsumer)}] ConvertMessageToJsonConsumer:RoutingKey is empty!");
            return;
        }

        if (string.IsNullOrWhiteSpace(queueKey))
        {
            logger.LogError(
                $"[{nameof(ConvertMessageToJsonConsumer)}] ConvertMessageToJsonConsumer:QueueKey is empty!");
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

    private static int _counter = 0;
    private static SemaphoreSlim _semaphore = new(47);

    private async Task Receive(IChannel channel, BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        if (_counter > 45)
        {
            _counter = 0;
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
        }

        await using var scope = serviceScopeFactory.CreateAsyncScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var message = Encoding.UTF8.GetString(ea.Body.ToArray());

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ConvertMessageToJsonConsumer>>();

        if (!string.IsNullOrWhiteSpace(message))
        {
            var result = await mediator.Send(new SendPostToAIParams(message), cancellationToken);
            try
            {
                var mongo = scope.ServiceProvider.GetRequiredService<IMongoConnectionManager>();
                var collection = mongo.GetCollection<PostsCollectionDto>(nameof(PostsCollectionDto));

                var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

                await collection.InsertManyAsync(mapper.Map<List<PostsCollectionDto>>(result.Posts),
                    cancellationToken: cancellationToken);

                await channel.BasicAckAsync(ea.DeliveryTag, false,
                    cancellationToken);

                _counter += 1;
            }
            catch (Exception e)
            {
                logger.LogError("[{0}] Insert post to mongo failed. Sending to DLX. Details: {1}",
                    nameof(ConvertMessageToJsonConsumer), e.Message);

                await channel.BasicNackAsync(ea.DeliveryTag, false, false,
                    cancellationToken);
            }
        }
    }
}