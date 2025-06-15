using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shashlik.EventBus.Utils;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

// ReSharper disable AsyncVoidLambda

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
            var channel = Connection.GetChannel();
            // 定义队列
            await channel.QueueDeclareAsync(eventHandlerDescriptor.EventHandlerName, true, false, false, cancellationToken: cancellationToken);
            // 绑定队列到交换机以及routing key
            await channel.QueueBindAsync(eventHandlerDescriptor.EventHandlerName, Options.CurrentValue.Exchange,
                eventHandlerDescriptor.EventName, cancellationToken: cancellationToken);

            var eventName = eventHandlerDescriptor.EventName;
            var eventHandlerName = eventHandlerDescriptor.EventHandlerName;

            var consumer = Connection.CreateConsumer(eventHandlerName);
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

                // 处理消息
                var res = await MessageListener
                    .OnReceiveAsync(eventHandlerName, message, cancellationToken)
                    .ConfigureAwait(false);
                if (res == MessageReceiveResult.Success)
                    // 一定要在消息接收处理完成后才确认ack
                    await channel.BasicAckAsync(e.DeliveryTag, false, cancellationToken);
                else
                    await channel.BasicRejectAsync(e.DeliveryTag, true, cancellationToken);
            };

            consumer.RegisteredAsync += async (_, _) =>
            {
                await Task.CompletedTask;
                Logger.LogInformation(
                    $"[EventBus-RabbitMQ] event handler \"{eventHandlerName}\" has been registered");
            };
            consumer.ShutdownAsync += async (_, _) =>
            {
                await Task.CompletedTask;

                Logger.LogWarning(
                    $"[EventBus-RabbitMQ] event handler \"{eventHandlerName}\" has been shutdown");
            };
            await channel.BasicConsumeAsync(eventHandlerName, false, consumer, cancellationToken: cancellationToken);
        }
    }
}