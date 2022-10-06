using MongoDB.Driver;

namespace Shashlik.EventBus.MongoDb
{
    public interface IConnection
    {
        IMongoClient Client { get; }
    }
}