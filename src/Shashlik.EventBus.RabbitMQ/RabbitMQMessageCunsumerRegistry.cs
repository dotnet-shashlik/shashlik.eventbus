using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Shashlik.EventBus.RabbitMQ
{
    public class RabbitMQMessageCunsumerRegistry : IMessageCunsumerRegistry
    {
        public RabbitMQMessageCunsumerRegistry(IOptionsMonitor<EventBusRabbitMQOptions> options,
            IRabbitMQConnection connection, ILogger<RabbitMQMessageCunsumerRegistry> logger,
            IMessageSerializer messageSerializer)
        {
            Options = options;
            Logger = logger;
            MessageSerializer = messageSerializer;
            Channel = connection.GetChannel();
        }

        private IOptionsMonitor<EventBusRabbitMQOptions> Options { get; }
        private IModel Channel { get; }
        private ILogger<RabbitMQMessageCunsumerRegistry> Logger { get; }
        private IMessageSerializer MessageSerializer { get; }

        public void Subscribe(IMessageListener listener, CancellationToken cancellationToken)
        {
            // 注册基础通信交换机
            Channel.ExchangeDeclare(Options.CurrentValue.Exchange, "topic", true);
            // 注册死信交换机
            Channel.ExchangeDeclare(Options.CurrentValue.DeadExchange, "topic", true);
            // 如果是延迟队列,先定义死信交换机
            Channel.QueueDeclare(listener.Descriptor.EventHandlerName, true, false, false);

            // 延迟队列需要配置死信队列
            if (listener.Descriptor.IsDelay)
            {
                // 配置延迟队列
                var delayQueue = $"{listener.Descriptor.EventHandlerName}.DELAY";
                var map = new Dictionary<string, object>
                {
                    {"x-dead-letter-exchange", Options.CurrentValue.DeadExchange},
                    {"x-dead-letter-routing-key", listener.Descriptor.EventName}
                };
                // 如果是延迟队列,先定义死信交换机
                Channel.QueueDeclare(delayQueue, true, false, false, map);

                // 如果是延迟任务,队列绑定到死信交换机
                Channel.QueueBind(listener.Descriptor.EventHandlerName, Options.CurrentValue.DeadExchange,
                    listener.Descriptor.EventName);
                // 如果是延迟任务,延迟队列绑定正常的交换机
                Channel.QueueBind(delayQueue, Options.CurrentValue.Exchange, listener.Descriptor.EventName);
            }
            else
            {
                Channel.QueueBind(listener.Descriptor.EventHandlerName, Options.CurrentValue.Exchange,
                    listener.Descriptor.EventName);
            }

            var consumer = new EventingBasicConsumer(Channel);
            consumer.Received += (sender, e) =>
            {
                var body = Encoding.UTF8.GetString(e.Body);
                var message =
                    MessageSerializer.Deserialize(body, typeof(MessageTransferModel)) as
                        MessageTransferModel;

                Logger.LogDebug(
                    $"{DateTime.Now}: [EventBus-RabbitMQ] received msg: {body}.");

                listener.Receive(message, cancellationToken);
                // 一定要在消息接收处理完成后才确认ack
                Channel.BasicAck(e.DeliveryTag, false);
            };

            consumer.Registered += (sender, e) =>
            {
                Logger.LogInformation(
                    $"{DateTime.Now}: [EventBus-RabbitMQ] event handler \"{listener.Descriptor.EventHandlerName}\" has been registered.");
            };
            consumer.Shutdown += (sender, e) =>
            {
                Logger.LogWarning(
                    $"{DateTime.Now}: [EventBus-RabbitMQ] event handler \"{listener.Descriptor.EventHandlerName}\" has been shutdown.");
            };
            consumer.Unregistered += (sender, e) =>
            {
                Logger.LogWarning(
                    $"{DateTime.Now}: [EventBus-RabbitMQ] event handler \"{listener.Descriptor.EventHandlerName}\" has been unregistered.");
            };
            consumer.ConsumerCancelled += (sender, e) =>
            {
                Logger.LogWarning(
                    $"{DateTime.Now}: [EventBus-RabbitMQ] event handler \"{listener.Descriptor.EventHandlerName}\" has been cancelled.");
            };

            Channel.BasicConsume(listener.Descriptor.EventHandlerName, false, consumer);
        }
    }
}