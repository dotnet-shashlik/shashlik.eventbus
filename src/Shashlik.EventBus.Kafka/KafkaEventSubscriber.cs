using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.Kafka
{
    public class KafkaEventSubscriber : IEventSubscriber
    {
        public KafkaEventSubscriber(IKafkaConnection connection, IMessageSerializer messageSerializer,
            ILogger<KafkaEventSubscriber> logger)
        {
            Connection = connection;
            MessageSerializer = messageSerializer;
            Logger = logger;
        }

        private IKafkaConnection Connection { get; }
        private IMessageSerializer MessageSerializer { get; }
        private ILogger<KafkaEventSubscriber> Logger { get; }

        public Task Subscribe(IMessageListener listener, CancellationToken cancellationToken)
        {
            var _ = Task.Run(async () =>
            {
                var cunsumer = Connection.CreateCunsumer(listener.Descriptor.EventHandlerName);
                cunsumer.Subscribe(listener.Descriptor.EventName);
                while (!cancellationToken.IsCancellationRequested)
                {
                    var consumerResult = cunsumer.Consume(cancellationToken);
                    if (consumerResult.IsPartitionEOF || consumerResult.Message.Value.IsNullOrEmpty()) continue;

                    var __ = Task.Run(async () =>
                    {
                        MessageTransferModel message;
                        try
                        {
                            message = MessageSerializer.Deserialize<MessageTransferModel>(consumerResult.Message.Value);
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(e, "[EventBus-Kafka] deserialize message from kafka error.");
                            return;
                        }

                        if (message == null)
                        {
                            Logger.LogError("[EventBus-Kafka] deserialize message from kafka error.");
                            return;
                        }

                        if (message.EventName != listener.Descriptor.EventName)
                        {
                            Logger.LogError(
                                $"[EventBus-Kafka] received invalid event name \"{message.EventName}\", expect \"{listener.Descriptor.EventName}\".");
                            return;
                        }

                        Logger.LogDebug(
                            $"[EventBus-Kafka] received msg: {message.ToJson()}.");

                        try
                        {
                            // 处理消息
                            await listener.OnReceive(message, cancellationToken).ConfigureAwait(false);
                            // 存储偏移,提交消息, see: https://docs.confluent.io/current/clients/dotnet.html
                            cunsumer.StoreOffset(consumerResult);
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(e,
                                $"[EventBus-Kafka] received msg execute OnReceive error: {message.ToJson()}.");
                        }
                    }, cancellationToken);

                    await Task.Delay(5, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }
    }
}