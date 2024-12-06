using Microsoft.Extensions.DependencyInjection;

namespace TL.Shared.Core.Mongo;

public static class MongoConnectionManagerExtension
{
    public static IServiceCollection AddMongoConnectionManager(this IServiceCollection services)
    {
        services.AddSingleton<IMongoConnectionManager, MongoConnectionManager>();

        return services;
    }
}