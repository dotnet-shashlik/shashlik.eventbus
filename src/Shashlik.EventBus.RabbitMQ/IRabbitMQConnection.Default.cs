﻿using System;
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
            Console.WriteLine($"Thread id: {id}");
            return Channels.GetOrAdd(id, r => Connection.CreateModel());
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
                    Port = Options.CurrentValue.Port
                };
            return factory.CreateConnection();
        }

        public void Dispose()
        {
            foreach (var item in Channels.Values)
            {
                item.Dispose();
            }

            Connection.Dispose();
            Channels.Clear();
        }
    }
}