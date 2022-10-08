using System;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Shashlik.EventBus.MongoDb
{
    public class DefaultConnection : IConnection
    {
        public DefaultConnection(
            IOptions<EventBusMongoDbOptions> options)
        {
            Options = options.Value;
            _connectionString = new Lazy<IMongoClient>(Get);
        }

        private readonly Lazy<IMongoClient> _connectionString;
        private EventBusMongoDbOptions Options { get; }
        public IMongoClient Client => _connectionString.Value;

        public IMongoClient Get()
        {
            return new MongoClient(Options.ConnectionString);
        }
    }
}