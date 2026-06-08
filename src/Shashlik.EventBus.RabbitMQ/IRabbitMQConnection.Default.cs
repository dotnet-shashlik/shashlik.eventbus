using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        private static readonly CreateChannelOptions ChannelOptions = new(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);

        public async ValueTask<IChannel> GetChannelAsync(CancellationToken cancellationToken = default)
        {
            var id = Environment.CurrentManagedThreadId;
            var channel = _channels.GetOrAdd(id, _ => CreateChannel());

            if (!channel.IsClosed)
                return channel;

            // 已关闭:清理并重建
            try
            {
                await channel.DisposeAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            channel = CreateChannel();
            _channels[id] = channel;
            return channel;
        }

        private IChannel CreateChannel()
        {
            var channel = Connection.CreateChannelAsync(ChannelOptions, CancellationToken.None)
                .GetAwaiter().GetResult();
            InitChannel(channel);
            return channel;
        }

        private void InitChannel(IChannel channel)
        {
            channel.BasicReturnAsync += (_, args) =>
            {
                var counter = args.BasicProperties.Headers.GetOrDefault(FailRetryHeaderKey);
                var counterInt = counter?.ParseTo<int>() ?? 0;
                if (counterInt >= 60)
                {
                    _logger.LogError(
                        $"[EventBus-RabbitMQ] send msg was returned and will not be try again: {args.RoutingKey}, ReplyCode: {args.ReplyCode}, ReplyText: {args.ReplyText}");
                    return Task.CompletedTask;
                }

                args.BasicProperties.Headers[FailRetryHeaderKey] = counterInt + 1;
                _logger.LogWarning(
                    $"[EventBus-RabbitMQ] send msg was returned and will try again: {args.RoutingKey}, ReplyCode: {args.ReplyCode}, ReplyText: {args.ReplyText}");

                // 7.x: args.BasicProperties 是只读 IReadOnlyBasicProperties,BasicPublishAsync 需要
                // 实现了 IReadOnlyBasicProperties + IAmqpHeader 的具体类型。BasicProperties 满足,
                // 但 Headers 是 IDictionary<string, object?>,需要复制过来。
                var newProperties = new BasicProperties
                {
                    MessageId = args.BasicProperties.MessageId,
                    Persistent = args.BasicProperties.Persistent,
                    ContentType = args.BasicProperties.ContentType,
                    ContentEncoding = args.BasicProperties.ContentEncoding,
                    Headers = args.BasicProperties.Headers != null
                        ? new Dictionary<string, object?>(args.BasicProperties.Headers)
                        : new Dictionary<string, object?>(),
                };
                // 被退回的消息重试发送
                channel.BasicPublishAsync(Options.CurrentValue.Exchange, args.RoutingKey, true, newProperties,
                    args.Body, CancellationToken.None).GetAwaiter().GetResult();
                return Task.CompletedTask;
            };
            // 交换机定义,类型topic
            channel.ExchangeDeclareAsync(Options.CurrentValue.Exchange, ExchangeType.Direct, true, false,
                cancellationToken: CancellationToken.None).GetAwaiter().GetResult();
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
                foreach (var item in _channels.Values)
                {
                    await item.DisposeAsync();
                }

                if (_connection.IsValueCreated)
                    await Connection.DisposeAsync();
                _channels.Clear();
            }
            catch
            {
                // ignore
            }
        }
    }
}
