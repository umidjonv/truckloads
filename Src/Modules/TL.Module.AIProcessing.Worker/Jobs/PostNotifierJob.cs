using System.Threading;
using System.Threading.Tasks;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using TL.Module.Telegram.Bot.Consumer;
using TL.Shared.Common.Dtos.AIProcessing;
using TL.Shared.Common.Dtos.Telegram;
using TL.Shared.Core.MessageBroker;
using TL.Shared.Core.Mongo;

namespace TL.Module.AIProcessing.Worker.Jobs;

public class PostNotifierJob(IServiceScopeFactory serviceScopeFactory, ILogger<PostNotifierJob> logger)
    : IPostNotifierJob
{
    public async Task Invoke(CancellationToken cancellationToken = default)
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

        var mongo = scope.ServiceProvider.GetRequiredService<IMongoConnectionManager>();
        var collection = mongo.GetCollection<PostsCollectionDto>(nameof(PostsCollectionDto));

        var filterDefinitionBuilder = Builders<PostsCollectionDto>.Filter;
        var filter = filterDefinitionBuilder.Eq(s => s.IsProcessed, false);
        using var cursor = await collection.FindAsync(filter, cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
            foreach (var post in cursor.Current)
            {
                var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
                var body = mapper.Map<NewPostParams>(post);
                await rabbit.PublishAsync(exchangeKey, routingKey, queueKey, body, cancellationToken);

                post.IsProcessed = true;
                var replaceFilter = Builders<PostsCollectionDto>.Filter.Eq(p => p.Id, post.Id);
                await collection.ReplaceOneAsync(replaceFilter, post, cancellationToken: cancellationToken);
            }
    }
}