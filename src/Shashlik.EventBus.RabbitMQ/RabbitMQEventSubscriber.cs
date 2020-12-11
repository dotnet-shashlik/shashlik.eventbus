using System;
using System.Threading;
using System.Threading.Tasks;
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
            IMessageSerializer messageSerializer, IMessageListener messageListener)
        {
            Options = options;
            Connection = connection;
            Logger = logger;
            MessageSerializer = messageSerializer;
            MessageListener = messageListener;
            Channel = connection.GetChannel();
        }

        private IOptionsMonitor<EventBusRabbitMQOptions> Options { get; }
        private IModel Channel { get; }
        private ILogger<RabbitMQEventSubscriber> Logger { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IMessageListener MessageListener { get; }
        private IRabbitMQConnection Connection { get; }

        public Task Subscribe(EventHandlerDescriptor eventHandlerDescriptor, CancellationToken cancellationToken)
        {
            // 注册基础通信交换机,类型topic
            Channel.ExchangeDeclare(Options.CurrentValue.Exchange, "topic", true);
            // 定义队列
            Channel.QueueDeclare(eventHandlerDescriptor.EventHandlerName, true, false, false);
            // 绑定队列到交换机以及routing key
            Channel.QueueBind(eventHandlerDescriptor.EventHandlerName, Options.CurrentValue.Exchange,
                eventHandlerDescriptor.EventName);

            var eventName = eventHandlerDescriptor.EventName;
            var eventHandlerName = eventHandlerDescriptor.EventHandlerName;

            var consumer = Connection.CreateConsumer(eventHandlerName);
            consumer.Received += async (sender, e) =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                MessageTransferModel message;
                try
                {
                    message = MessageSerializer.Deserialize<MessageTransferModel>(e.Body.ToArray());
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

                if (message.EventName != eventName)
                {
                    Logger.LogError(
                        $"[EventBus-RabbitMQ] received invalid event name \"{message.EventName}\", expect \"{eventName}\".");
                    return;
                }

                Logger.LogDebug(
                    $"[EventBus-RabbitMQ] received msg: {message.ToJson()}.");

                // 处理消息
                var res = await MessageListener.OnReceive(eventHandlerName, message, cancellationToken).ConfigureAwait(false);
                if (res == MessageReceiveResult.Success)
                    // 一定要在消息接收处理完成后才确认ack
                    Channel.BasicAck(e.DeliveryTag, false);
                else
                    Channel.BasicReject(e.DeliveryTag, true);
            };

            consumer.Registered += (sender, e) =>
            {
                Logger.LogInformation(
                    $"[EventBus-RabbitMQ] event handler \"{eventHandlerName}\" has been registered.");
            };
            consumer.Shutdown += (sender, e) =>
            {
                Logger.LogWarning(
                    $"[EventBus-RabbitMQ] event handler \"{eventHandlerName}\" has been shutdown.");
            };
            Channel.BasicConsume(eventHandlerName, false, consumer);

            return Task.CompletedTask;
        }
    }
}