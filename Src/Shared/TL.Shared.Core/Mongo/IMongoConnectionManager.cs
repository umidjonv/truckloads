using MongoDB.Driver;

namespace TL.Shared.Core.Mongo;

public interface IMongoConnectionManager
{
    IMongoDatabase Database { get; set; }

    IMongoCollection<T> GetCollection<T>(string collectionName);
}