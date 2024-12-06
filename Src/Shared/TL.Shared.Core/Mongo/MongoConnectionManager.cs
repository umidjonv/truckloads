using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace TL.Shared.Core.Mongo;

public class MongoConnectionManager : IMongoConnectionManager
{
    public IMongoDatabase Database { get; set; }

    public MongoConnectionManager(IConfigurationManager configurationManager)
    {
        var client = new MongoClient(configurationManager.GetConnectionString("MongoConnectionString"));
        Database = client.GetDatabase(configurationManager.GetConnectionString("Mongo:Database"));
    }

    public IMongoCollection<T> GetCollection<T>(string collectionName)
    {
        return Database.GetCollection<T>(collectionName);
    }
}