using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
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
                              ?? throw new EventBusException(
                                  "[EventBus-RabbitMQ] failed to rent channel for subscribe");
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


            var eventName = eventHandlerDescriptor.EventName;
            var eventHandlerName = eventHandlerDescriptor.EventHandlerName;
            var poolSize = Math.Max(1, Options.CurrentValue.ConsumerPoolSize);

            // 跟踪所有 consumer 的 channel 租约,用于 StopAsync 时 close
            // (long-lived consumer 独占 channel,不参与池的租借/归还)
            for (var i = 0; i < poolSize; i++)
            {
                var consumer = Connection.CreateConsumer(eventHandlerName);
                var consumerChannel = consumer.Channel;
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
                            await consumerChannel.BasicAckAsync(e.DeliveryTag, false, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "[EventBus-RabbitMQ] ack failed, channel may be closed");
                        }
                    }
                    else
                    {
                        try
                        {
                            await consumerChannel.BasicRejectAsync(e.DeliveryTag, true, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "[EventBus-RabbitMQ] nack failed, channel may be closed");
                        }
                    }
                };

                consumer.ShutdownAsync += (_, e) =>
                {
                    Logger.LogWarning(
                        $"[EventBus-RabbitMQ] event handler \"{eventHandlerName}\" consumer #{i} has been shutdown, initiator: {e.Initiator}, replyCode: {e.ReplyCode}, replyText: {e.ReplyText}");
                    return Task.CompletedTask;
                };

                await consumerChannel.BasicConsumeAsync(eventHandlerName, false,
                        $"{eventHandlerName}#{i}", false, false, null, consumer, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}