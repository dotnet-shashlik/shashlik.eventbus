using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Shashlik.EventBus.RabbitMQ
{
    public class DefaultRabbitMQConnection : IRabbitMQConnection, IDisposable
    {
        private static readonly ConcurrentDictionary<int, IModel> Channels = new ConcurrentDictionary<int, IModel>();

        public DefaultRabbitMQConnection(IOptionsMonitor<EventBusRabbitMQOptions> options)
        {
            Options = options;
            _connection = new Lazy<IConnection>(Get);
        }

        private IOptionsMonitor<EventBusRabbitMQOptions> Options { get; }
        private readonly Lazy<IConnection> _connection;
        private IConnection Connection => _connection.Value;

        public IModel GetChannel()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            var channel = Channels.GetOrAdd(id, r => Connection.CreateModel());
            if (channel.IsClosed)
            {
                channel.Dispose();
                channel = Channels[id] = Connection.CreateModel();
            }

            return channel;
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
            return factory.CreateConnection();
        }

        public void Dispose()
        {
            try
            {
                foreach (var item in Channels.Values)
                {
                    item.Dispose();
                }

                Connection.Dispose();
                Channels.Clear();
            }
            catch
            {
                // ignore
            }
        }
    }
}