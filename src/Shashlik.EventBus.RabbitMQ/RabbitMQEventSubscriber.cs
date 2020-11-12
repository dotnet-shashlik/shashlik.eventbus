using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.RabbitMQ
{
    public class RabbitMQEventSubscriber : IEventSubscriber
    {
        public RabbitMQEventSubscriber(IOptionsMonitor<EventBusRabbitMQOptions> options,
            IRabbitMQConnection connection, ILogger<RabbitMQEventSubscriber> logger,
            IMessageSerializer messageSerializer)
        {
            Options = options;
            Logger = logger;
            MessageSerializer = messageSerializer;
            Channel = connection.GetChannel();
        }

        private IOptionsMonitor<EventBusRabbitMQOptions> Options { get; }
        private IModel Channel { get; }
        private ILogger<RabbitMQEventSubscriber> Logger { get; }
        private IMessageSerializer MessageSerializer { get; }

        public void Subscribe(IMessageListener listener, CancellationToken cancellationToken)
        {
            // 注册基础通信交换机,类型topic
            Channel.ExchangeDeclare(Options.CurrentValue.Exchange, "topic", true);
            // 定义队列
            Channel.QueueDeclare(listener.Descriptor.EventHandlerName, true, false, false);
            // 绑定队列到交换机以及routing key
            Channel.QueueBind(listener.Descriptor.EventHandlerName, Options.CurrentValue.Exchange,
                listener.Descriptor.EventName);

            var consumer = new EventingBasicConsumer(Channel);
            consumer.Received += (sender, e) =>
            {
                MessageTransferModel message;
                try
                {
                    message = MessageSerializer.Deserialize<MessageTransferModel>(e.Body);
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "[EventBus-RabbitMQ] deserialize message from rabbit error.");
                    return;
                }

                if (message == null)
                {
                    Logger.LogError("[EventBus-RabbitMQ] deserialize message from rabbit error.");
                    return;
                }

                if (message.EventName != listener.Descriptor.EventName)
                {
                    Logger.LogError(
                        $"[EventBus-RabbitMQ] received invalid event name \"{message.EventName}\", expect \"{listener.Descriptor.EventName}\".");
                    return;
                }

                Logger.LogDebug(
                    $"[EventBus-RabbitMQ] received msg: {message.ToJson()}.");

                try
                {
                    // 处理消息
                    listener.OnReceive(message, cancellationToken);
                    // 一定要在消息接收处理完成后才确认ack
                    Channel.BasicAck(e.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex,
                        $"[EventBus-Kafka] received msg execute OnReceive error: {message.ToJson()}.");
                }
            };

            consumer.Registered += (sender, e) =>
            {
                Logger.LogInformation(
                    $"[EventBus-RabbitMQ] event handler \"{listener.Descriptor.EventHandlerName}\" has been registered.");
            };
            consumer.Shutdown += (sender, e) =>
            {
                Logger.LogWarning(
                    $"[EventBus-RabbitMQ] event handler \"{listener.Descriptor.EventHandlerName}\" has been shutdown.");
            };
            consumer.Unregistered += (sender, e) =>
            {
                Logger.LogWarning(
                    $"[EventBus-RabbitMQ] event handler \"{listener.Descriptor.EventHandlerName}\" has been unregistered.");
            };
            consumer.ConsumerCancelled += (sender, e) =>
            {
                Logger.LogWarning(
                    $"[EventBus-RabbitMQ] event handler \"{listener.Descriptor.EventHandlerName}\" has been cancelled.");
            };

            Channel.BasicConsume(listener.Descriptor.EventHandlerName, false, consumer);
        }
    }
}