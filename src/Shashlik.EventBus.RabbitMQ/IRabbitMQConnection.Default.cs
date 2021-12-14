using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Shashlik.EventBus.RabbitMQ
{
    public class DefaultRabbitMQConnection : IRabbitMQConnection, IDisposable
    {
        public DefaultRabbitMQConnection(IOptionsMonitor<EventBusRabbitMQOptions> options)
        {
            Options = options;
            _connection = new Lazy<IConnection>(Get);
            _channels = new ConcurrentDictionary<int, IModel>();
            _consumers = new ConcurrentDictionary<string, EventingBasicConsumer>();
        }

        private readonly Lazy<IConnection> _connection;
        private readonly ConcurrentDictionary<int, IModel> _channels;
        private readonly ConcurrentDictionary<string, EventingBasicConsumer> _consumers;
        private IOptionsMonitor<EventBusRabbitMQOptions> Options { get; }
        private IConnection Connection => _connection.Value;

        public IModel GetChannel()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            var channel = _channels.GetOrAdd(id, r =>
            {
                var c = Connection.CreateModel();
                c.ConfirmSelect();
                return c;
            });
            if (channel.IsClosed)
            {
                channel.Dispose();
                channel = _channels[id] = Connection.CreateModel();
                channel.ConfirmSelect();
            }

            return channel;
        }

        public EventingBasicConsumer CreateConsumer(string eventHandlerName)
        {
            return _consumers.GetOrAdd(eventHandlerName, r => new EventingBasicConsumer(GetChannel()));
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
                foreach (var item in _channels.Values)
                {
                    item.Dispose();
                }

                Connection.Dispose();
                _channels.Clear();
            }
            catch
            {
                // ignore
            }
        }
    }
}