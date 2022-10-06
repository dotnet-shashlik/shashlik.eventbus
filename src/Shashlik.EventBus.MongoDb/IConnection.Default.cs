using System;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Shashlik.EventBus.MongoDb
{
    public class DefaultConnection : IConnection
    {
        public DefaultConnection(
            IOptions<EventBusMongoDbOptions> options, IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            Options = options.Value;
            _connectionString = new Lazy<IMongoClient>(Get);
        }

        private readonly Lazy<IMongoClient> _connectionString;
        private EventBusMongoDbOptions Options { get; }
        private IServiceProvider ServiceProvider { get; }
        public IMongoClient Client => _connectionString.Value;

        public IMongoClient Get()
        {
            return Options.ClientFactory is not null
                ? Options.ClientFactory(ServiceProvider)
                : new MongoClient(Options.ConnectionString);
        }
    }
}