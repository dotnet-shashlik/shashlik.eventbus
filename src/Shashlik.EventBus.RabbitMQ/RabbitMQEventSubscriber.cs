using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.RabbitMQ
{
    public class RabbitMQEventSubscriber : IEventSubscriber
    {
        public RabbitMQEventSubscriber(IOptionsMonitor<EventBusRabbitMQOptions> options,
            IRabbitMQConnection connection, ILogger<RabbitMQEventSubscriber> logger,
            IMessageSerializer messageSerializer, IMessageListener messageListener)
        {
            Options = options;
            Connection = connection;
            Logger = logger;
            MessageSerializer = messageSerializer;
            MessageListener = messageListener;
        }

        private IOptionsMonitor<EventBusRabbitMQOptions> Options { get; }
        private ILogger<RabbitMQEventSubscriber> Logger { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IMessageListener MessageListener { get; }
        private IRabbitMQConnection Connection { get; }

        public async Task SubscribeAsync(EventHandlerDescriptor eventHandlerDescriptor,
            CancellationToken cancellationToken)
        {
            // 借一个 channel 用于 declare + bind,declare 是 idempotent,
            // 离开作用域后 channel 归还到池供 publish 路径复用。
            await using (var lease = await Connection.GetChannelAsync(cancellationToken).ConfigureAwait(false))
            {
                var channel = lease.Value
                    ?? throw new EventBusException("[EventBus-RabbitMQ] failed to rent channel for subscribe");
                await channel.ExchangeDeclareAsync(Options.CurrentValue.Exchange, ExchangeType.Topic, true, false,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                await channel.QueueDeclareAsync(eventHandlerDescriptor.EventHandlerName, true, false, false,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                await channel.QueueBindAsync(eventHandlerDescriptor.EventHandlerName,
                    Options.CurrentValue.Exchange, eventHandlerDescriptor.EventName,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            // 再借一个 channel 给 consumer 长期持有。注意 consumer 是和 broker 之间的
            // 长连接,不能像 publish 那样"用完即还"。这里我们在 EventBusStartup.Build
            // 流程中创建 N 个 channel(每个 handler 一个),N 由 consumer 数决定。
            // 由于 consumer 必须独占 channel,且不能跨 handler 复用,我们这里用独立 channel。
            await using var consumerLease = await Connection.GetChannelAsync(cancellationToken)
                .ConfigureAwait(false);
            var consumerChannel = consumerLease.Value
                ?? throw new EventBusException("[EventBus-RabbitMQ] failed to rent channel for consumer");

            var eventName = eventHandlerDescriptor.EventName;
            var eventHandlerName = eventHandlerDescriptor.EventHandlerName;

            var consumer = Connection.CreateConsumer(eventHandlerName, consumerChannel);
            consumer.ReceivedAsync += async (_, e) =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                MessageTransferModel? message;
                try
                {
                    message = MessageSerializer.Deserialize<MessageTransferModel>(e.Body.ToArray());
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "[EventBus-RabbitMQ] deserialize message from rabbit error");
                    return;
                }

                if (message is null)
                {
                    Logger.LogError("[EventBus-RabbitMQ] deserialize message from rabbit error");
                    return;
                }

                if (message.EventName != eventName)
                {
                    Logger.LogError(
                        $"[EventBus-RabbitMQ] received invalid event name \"{message.EventName}\", expect \"{eventName}\"");
                    return;
                }

                Logger.LogDebug(
                    $"[EventBus-RabbitMQ] received msg: {message}");

                var res = await MessageListener
                    .OnReceiveAsync(eventHandlerName, message, cancellationToken)
                    .ConfigureAwait(false);
                if (res == MessageReceiveResult.Success)
                {
                    try
                    {
                        await consumerChannel.BasicAckAsync(e.DeliveryTag, false).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // channel 已关闭(借出/归还期间被 close),直接丢弃 lease 让池丢弃对象
                        consumerLease.IsValid = false;
                        Logger.LogError(ex, "[EventBus-RabbitMQ] ack failed, channel may be closed");
                    }
                }
                else
                {
                    try
                    {
                        await consumerChannel.BasicRejectAsync(e.DeliveryTag, true).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        consumerLease.IsValid = false;
                        Logger.LogError(ex, "[EventBus-RabbitMQ] nack failed, channel may be closed");
                    }
                }
            };

            consumer.ShutdownAsync += (_, e) =>
            {
                Logger.LogWarning(
                    $"[EventBus-RabbitMQ] event handler \"{eventHandlerName}\" has been shutdown, initiator: {e.Initiator}, replyCode: {e.ReplyCode}, replyText: {e.ReplyText}");
                return Task.CompletedTask;
            };

            await consumerChannel.BasicConsumeAsync(eventHandlerName, false, eventHandlerName, false, false,
                    null, consumer, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
