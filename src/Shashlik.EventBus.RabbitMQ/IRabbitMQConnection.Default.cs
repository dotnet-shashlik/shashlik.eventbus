using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.RabbitMQ
{
    public class DefaultRabbitMQConnection : IRabbitMQConnection, IAsyncDisposable
    {
        public DefaultRabbitMQConnection(
            IOptionsMonitor<EventBusRabbitMQOptions> options,
            IObjectPoolProvider poolProvider,
            ILogger<RabbitMQMessageSender> logger)
        {
            Options = options;
            _logger = logger;
            _connection = new Lazy<IConnection>(Get, true);
            _consumers = new ConcurrentDictionary<string, AsyncEventingBasicConsumer>();

            // channel 池: 软上限 = 16。够应付一般应用并发,不至于撑爆 broker fd。
            // 真正高并发场景由用户通过 EventBusRabbitMQOptions 调整。
            _channelPool = poolProvider.Create(
                "rabbitmq.channel",
                new RabbitMQConnectionPoolPolicy(options, _connection, options.CurrentValue.Exchange),
                maxSize: Math.Max(1, Environment.ProcessorCount * 2));
        }

        private readonly Lazy<IConnection> _connection;
        private readonly IObjectPool<IChannel> _channelPool;
        private readonly ConcurrentDictionary<string, AsyncEventingBasicConsumer> _consumers;
        private readonly ILogger<RabbitMQMessageSender> _logger;

        private IOptionsMonitor<EventBusRabbitMQOptions> Options { get; }

        public async ValueTask<IPoolLease<IChannel>> GetChannelAsync(CancellationToken cancellationToken = default)
        {
            return await _channelPool.RentAsync(cancellationToken).ConfigureAwait(false);
        }

        public AsyncEventingBasicConsumer CreateConsumer(string eventHandlerName, IChannel channel)
        {
            return _consumers.GetOrAdd(eventHandlerName, _ => new AsyncEventingBasicConsumer(channel));
        }

        private IConnection Get()
        {
            ConnectionFactory factory;
            if (Options.CurrentValue.ConnectionFactory != null)
                factory = Options.CurrentValue.ConnectionFactory();
            else
                factory = new ConnectionFactory
                {
                    Password = Options.CurrentValue.Password,
                    HostName = Options.CurrentValue.Host,
                    UserName = Options.CurrentValue.UserName,
                    Port = Options.CurrentValue.Port,
                    VirtualHost = Options.CurrentValue.VirtualHost
                };
            return factory.CreateConnectionAsync().GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_connection.IsValueCreated)
                    await _connection.Value.DisposeAsync();
            }
            catch
            {
                // ignore
            }
        }
    }
}
