using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using TL.Shared.Common.Dtos.AIProcessing;
using TL.Shared.Core.MessageBroker;
using TL.Shared.Core.Mongo;

namespace TL.Module.AIProcessing.Worker.Jobs;

public class PostNotifierJob(IServiceScopeFactory serviceScopeFactory) : IPostNotifierJob
{
    public async Task Invoke(CancellationToken cancellationToken = default)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var mongo = scope.ServiceProvider.GetRequiredService<IMongoConnectionManager>();
        var collection = mongo.GetCollection<PostsCollectionDto>(nameof(PostsCollectionDto));

        var filterDefinitionBuilder = Builders<PostsCollectionDto>.Filter;
        var filter = filterDefinitionBuilder.Eq(s => s.IsProcessed, false);
        using var cursor = await collection.FindAsync(filter, cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var post in cursor.Current)
            {
                //TODO: notify subscribers
            }
        }
    }
}