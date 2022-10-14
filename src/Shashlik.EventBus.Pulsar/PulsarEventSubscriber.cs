using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pulsar.Client;
using Pulsar.Client.Api;
using Shashlik.EventBus.Pulsar;
using Shashlik.EventBus.Utils;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

// ReSharper disable AsyncVoidLambda

namespace Shashlik.EventBus.Pulsar
{
    public class PulsarEventSubscriber : IEventSubscriber
    {
        public PulsarEventSubscriber(IOptionsMonitor<EventBusPulsarOptions> options,
            IPulsarConnection connection, ILogger<PulsarEventSubscriber> logger,
            IMessageSerializer messageSerializer, IMessageListener messageListener)
        {
            Options = options;
            Connection = connection;
            Logger = logger;
            MessageSerializer = messageSerializer;
            MessageListener = messageListener;
        }

        private IOptionsMonitor<EventBusPulsarOptions> Options { get; }
        private ILogger<PulsarEventSubscriber> Logger { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IMessageListener MessageListener { get; }
        private IPulsarConnection Connection { get; }

        public Task SubscribeAsync(EventHandlerDescriptor eventHandlerDescriptor, CancellationToken cancellationToken)
        {
            var eventName = eventHandlerDescriptor.EventName;
            var eventHandlerName = eventHandlerDescriptor.EventHandlerName;
            var consumer = Connection.GetConsumer(eventName, eventHandlerName);
            _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await Consume(consumer, eventName, eventHandlerName, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, $"[EventBus-Kafka] consume message occur error");
                    }

                    // ReSharper disable once MethodSupportsCancellation
                    await Task.Delay(10).ConfigureAwait(false);
                }
            }, cancellationToken);
            return Task.CompletedTask;
        }

        private async Task Consume(IConsumer<byte[]> consumer, string eventName, string eventHandlerName,
            CancellationToken cancellationToken)
        {
            var messageRes = await consumer.ReceiveAsync(cancellationToken);
            if (messageRes is null)
                return;
            MessageTransferModel? message;
            try
            {
                message = MessageSerializer.Deserialize<MessageTransferModel>(messageRes.Data);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "[EventBus-Pulsar] deserialize message from rabbit error");
                return;
            }

            if (message is null)
            {
                Logger.LogError("[EventBus-Pulsar] deserialize message from rabbit error");
                return;
            }

            if (message.EventName != eventName)
            {
                Logger.LogError(
                    $"[EventBus-Pulsar] received invalid event name \"{message.EventName}\", expect \"{eventName}\"");
                return;
            }

            Logger.LogDebug(
                $"[EventBus-Pulsar] received msg: {message}");

            // 处理消息
            var res = await MessageListener
                .OnReceiveAsync(eventHandlerName, message, cancellationToken)
                .ConfigureAwait(false);
            if (res == MessageReceiveResult.Success)
                // 一定要在消息接收处理完成后才确认ack
                await consumer.AcknowledgeAsync(messageRes.MessageId);
            else
                await consumer!.NegativeAcknowledge(messageRes.MessageId);
        }
    }
}