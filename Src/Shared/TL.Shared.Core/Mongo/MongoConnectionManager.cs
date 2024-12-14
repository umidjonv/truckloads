using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace TL.Shared.Core.Mongo;

public class MongoConnectionManager : IMongoConnectionManager
{
    public IMongoDatabase Database { get; set; }

    public MongoConnectionManager(IConfiguration configuration)
    {
        var client = new MongoClient(configuration.GetConnectionString("MongoConnectionString"));
        Database = client.GetDatabase(configuration.GetConnectionString("Mongo:Database"));
    }

    public IMongoCollection<T> GetCollection<T>(string collectionName)
    {
        return Database.GetCollection<T>(collectionName);
    }
}