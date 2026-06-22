using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.Utils;
using StackExchange.Redis;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

// ReSharper disable AsyncVoidLambda

namespace Shashlik.EventBus.StackExchangeRedis
{
    public class StackExchangeRedisMQEventSubscriber : IEventSubscriber
    {
        public StackExchangeRedisMQEventSubscriber(
            IOptionsMonitor<EventBusStackExchangeRedisOptions> options,
            ILogger<StackExchangeRedisMQEventSubscriber> logger,
            IMessageSerializer messageSerializer,
            IMessageListener messageListener,
            IServiceProvider serviceProvider)
        {
            Options = options;
            Logger = logger;
            MessageSerializer = messageSerializer;
            MessageListener = messageListener;
            ServiceProvider = serviceProvider;
        }

        private IOptionsMonitor<EventBusStackExchangeRedisOptions> Options { get; }
        private ILogger<StackExchangeRedisMQEventSubscriber> Logger { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IMessageListener MessageListener { get; }
        private IServiceProvider ServiceProvider { get; }

        public async Task SubscribeAsync(EventHandlerDescriptor eventHandlerDescriptor,
            CancellationToken cancellationToken)
        {
            var connection = Options.CurrentValue.ConnectionMultiplexerFactory?.Invoke(ServiceProvider)
                ?? throw new InvalidOperationException("EventBusStackExchangeRedisOptions.ConnectionMultiplexerFactory error");
            var database = connection.GetDatabase();

            var eventName = eventHandlerDescriptor.EventName;
            var eventHandlerName = eventHandlerDescriptor.EventHandlerName;
            var poolSize = Math.Max(1, Options.CurrentValue.ConsumerPoolSize);

            try
            {
                await database.StreamCreateConsumerGroupAsync(eventName, eventHandlerName, "0", createStream: true)
                    .ConfigureAwait(false);
            }
            catch (RedisServerException e) when (e.Message.Contains("BUSYGROUP", StringComparison.OrdinalIgnoreCase))
            {
                // consumer group already exists, ignore
            }

            // 启动 poolSize 个 consumer task，同一个 consumer group 内多消费者
            // Redis Streams 会自动将消息分发给组内不同的消费者
            for (var i = 0; i < poolSize; i++)
            {
                var consumerIndex = i;
                var consumerName = $"{eventHandlerName}#{consumerIndex}";
                _ = Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        StreamEntry[] readResult;
                        try
                        {
                            readResult = await database.StreamReadGroupAsync(
                                eventName,
                                eventHandlerName,
                                consumerName,
                                position: StreamPosition.NewMessages,
                                count: 1).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(e,
                                $"[EventBus-StackExchangeRedis] consume message occur error, event: {eventName}, handler: {consumerName}");
                            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                            continue;
                        }

                        if (readResult is null || readResult.Length == 0)
                        {
                            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                            continue;
                        }

                        var entry = readResult[0];
                        var body = entry[StackExchangeRedisConstants.StreamBodyFieldName];
                        if (body.IsNullOrEmpty)
                        {
                            Logger.LogWarning(
                                $"[EventBus-StackExchangeRedis] received empty message, event: {eventName}, handler: {consumerName}");
                            continue;
                        }

                        MessageTransferModel? message;
                        try
                        {
                            message = MessageSerializer.Deserialize<MessageTransferModel>(body.ToString());
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(e,
                                $"[EventBus-StackExchangeRedis] deserialize message from redis error, event: {eventName}, handler: {consumerName}, body: {body}");
                            continue;
                        }

                        if (message is null)
                        {
                            Logger.LogError("[EventBus-StackExchangeRedis] deserialize message from redis error");
                            continue;
                        }

                        if (message.EventName != eventName)
                        {
                            Logger.LogError(
                                $"[EventBus-StackExchangeRedis] received invalid event name \"{message.EventName}\", expect \"{eventName}\"");
                            continue;
                        }

                        Logger.LogDebug(
                            $"[EventBus-StackExchangeRedis] received msg: {message}");

                        var res = await MessageListener
                            .OnReceiveAsync(eventHandlerName, message, cancellationToken)
                            .ConfigureAwait(false);
                        if (res == MessageReceiveResult.Success)
                        {
                            await database.StreamAcknowledgeAsync(eventName, eventHandlerName, entry.Id)
                                .ConfigureAwait(false);
                        }

                        // ReSharper disable once MethodSupportsCancellation
                        await Task.Delay(10).ConfigureAwait(false);
                    }
                }, cancellationToken);
            }
        }
    }
}
