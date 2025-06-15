using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.RabbitMQ
{
    public class DefaultRabbitMQConnection : IRabbitMQConnection, IDisposable
    {
        public DefaultRabbitMQConnection(IOptionsMonitor<EventBusRabbitMQOptions> options,
            ILogger<RabbitMQMessageSender> logger)
        {
            Options = options;
            _logger = logger;
            _connection = new Lazy<IConnection>(Get, true);
            _channels = new ConcurrentDictionary<int, IChannel>();
            _consumers = new ConcurrentDictionary<string, AsyncEventingBasicConsumer>();
        }

        private readonly Lazy<IConnection> _connection;
        private readonly ConcurrentDictionary<int, IChannel> _channels;
        private readonly ConcurrentDictionary<string, AsyncEventingBasicConsumer> _consumers;
        private readonly ILogger<RabbitMQMessageSender> _logger;

        private IOptionsMonitor<EventBusRabbitMQOptions> Options { get; }
        private IConnection Connection => _connection.Value;
        private const string FailRetryHeaderKey = "FailCounter";

        public IChannel GetChannel()
        {
            var id = Environment.CurrentManagedThreadId;
            var channel = _channels.GetOrAdd(id, r =>
            {
                var c = Connection.CreateChannelAsync(new CreateChannelOptions(true, true)).ConfigureAwait(false)
                    .GetAwaiter().GetResult();
                InitChannel(c);
                return c;
            });
            if (!channel.IsClosed) return channel;
            try
            {
                channel.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            channel = _channels[id] = Connection.CreateChannelAsync(new CreateChannelOptions(true, true))
                .ConfigureAwait(false).GetAwaiter().GetResult();
            InitChannel(channel);

            return channel;
        }

        private void InitChannel(IChannel channel)
        {
            channel.BasicReturnAsync += async (_, args) =>
            {
                var counter = args.BasicProperties.Headers?.GetOrDefault(FailRetryHeaderKey);
                var counterInt = counter?.ParseTo<int>() ?? 0;
                if (counterInt >= 60)
                {
                    _logger.LogError(
                        $"[EventBus-RabbitMQ] send msg was returned and will not be try again: {args.RoutingKey}, ReplyCode: {args.ReplyCode}, ReplyText: {args.ReplyText}");
                    return;
                }

                _logger.LogWarning(
                    $"[EventBus-RabbitMQ] send msg was returned and will try again: {args.RoutingKey}, ReplyCode: {args.ReplyCode}, ReplyText: {args.ReplyText}");


                var pro = new BasicProperties(args.BasicProperties);
                pro.Headers ??= new Dictionary<string, object?>();
                pro.Headers.Add(FailRetryHeaderKey, counterInt + 1);
                // 被退回的消息重试发送
                await channel.BasicPublishAsync(Options.CurrentValue.Exchange, args.RoutingKey, true,
                    pro, args.Body);
            };
            // 交换机定义,类型topic
            channel.ExchangeDeclareAsync(Options.CurrentValue.Exchange, ExchangeType.Topic, true)
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public AsyncEventingBasicConsumer CreateConsumer(string eventHandlerName)
        {
            return _consumers.GetOrAdd(eventHandlerName, r => new AsyncEventingBasicConsumer(GetChannel()));
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
            return factory.CreateConnectionAsync().ConfigureAwait(false).GetAwaiter().GetResult();
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