using System;
using System.Threading;
using System.Threading.Tasks;
using FreeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.Utils;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

// ReSharper disable AsyncVoidLambda

namespace Shashlik.EventBus.Redis
{
    public class RedisMQEventSubscriber : IEventSubscriber
    {
        public RedisMQEventSubscriber(IOptionsMonitor<EventBusRedisMQOptions> options,
            ILogger<RedisMQEventSubscriber> logger,
            IMessageSerializer messageSerializer, IMessageListener messageListener, IServiceProvider serviceProvider)
        {
            Options = options;
            Logger = logger;
            MessageSerializer = messageSerializer;
            MessageListener = messageListener;
            ServiceProvider = serviceProvider;
        }

        private IOptionsMonitor<EventBusRedisMQOptions> Options { get; }
        private ILogger<RedisMQEventSubscriber> Logger { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IMessageListener MessageListener { get; }
        private IServiceProvider ServiceProvider { get; }

        public async Task SubscribeAsync(EventHandlerDescriptor eventHandlerDescriptor,
            CancellationToken cancellationToken)
        {
            var redisClient = Options.CurrentValue.RedisClientFactory?.Invoke(ServiceProvider);
            if (redisClient is null)
                throw new InvalidOperationException("invalid redis client factory");

            var eventName = eventHandlerDescriptor.EventName;
            var eventHandlerName = eventHandlerDescriptor.EventHandlerName;
            try
            {
                await redisClient.XGroupCreateAsync(eventName, eventHandlerName, MkStream: true);
            }
            catch (Exception e)
            {
                if (!e.Message.Contains("already exists"))
                    throw;
            }

            _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    StreamsEntry? readResult;
                    try
                    {
                        readResult =
                            await redisClient.XReadGroupAsync(eventHandlerName, eventHandlerName, 5000, eventName, ">");
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e,
                            $"[EventBus-Redis] consume message occur error, event: {eventName}, handler: {eventHandlerName}");
                        continue;
                    }

                    if (readResult is null)
                        continue;
                    if (readResult.fieldValues.Length >= 2 &&
                        readResult.fieldValues[0].ToString() == RedisConstants.StreamBodyFieldName)
                    {
                        var messageBody = readResult.fieldValues[1].ToString();
                        if (messageBody.IsNullOrWhiteSpace())
                        {
                            Logger.LogError(
                                $"[EventBus-Redis] received empty message, event: {eventName}, handler: {eventHandlerName}");
                            continue;
                        }

                        MessageTransferModel? message;

                        try
                        {
                            message = MessageSerializer.Deserialize<MessageTransferModel>(messageBody!);
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(e,
                                $"[EventBus-Redis] deserialize message from redis error, event: {eventName}, handler: {eventHandlerName}, body: {messageBody}");
                            continue;
                        }

                        if (message is null)
                        {
                            Logger.LogError("[EventBus-Redis] deserialize message from redis error");
                            return;
                        }

                        if (message.EventName != eventName)
                        {
                            Logger.LogError(
                                $"[EventBus-Redis] received invalid event name \"{message.EventName}\", expect \"{eventName}\"");
                            return;
                        }

                        Logger.LogDebug(
                            $"[EventBus-Redis] received msg: {message}");

                        // 处理消息
                        var res = await MessageListener
                            .OnReceiveAsync(eventHandlerName, message, cancellationToken)
                            .ConfigureAwait(false);
                        if (res == MessageReceiveResult.Success)
                        {
                            await redisClient.XAckAsync(eventName, eventHandlerName, readResult.id);
                        }

                        // ReSharper disable once MethodSupportsCancellation
                        await Task.Delay(5).ConfigureAwait(false);
                    }
                }
            }, cancellationToken);
        }
    }
}