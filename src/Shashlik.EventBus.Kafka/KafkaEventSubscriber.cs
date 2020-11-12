using System;
using System.Text;
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

        public void Subscribe(IMessageListener listener, CancellationToken cancellationToken)
        {
            Task.Run(() =>
            {
                var cunsumer = Connection.CreateCunsumer(listener.Descriptor.EventHandlerName);
                cunsumer.Subscribe(listener.Descriptor.EventName);
                while (!cancellationToken.IsCancellationRequested)
                {
                    var consumerResult = cunsumer.Consume(cancellationToken);
                    if (consumerResult.IsPartitionEOF || consumerResult.Message.Value.IsNullOrEmpty()) continue;

                    MessageTransferModel message;
                    try
                    {
                        message = MessageSerializer.Deserialize<MessageTransferModel>(consumerResult.Message.Value);
                    }
                    catch (Exception exception)
                    {
                        Logger.LogError("[EventBus-Kafka] deserialize message from kafka error.", exception);
                        continue;
                    }

                    if (message == null)
                    {
                        Logger.LogError("[EventBus-Kafka] deserialize message from kafka error.");
                        continue;
                    }

                    if (message.EventName != listener.Descriptor.EventName)
                    {
                        Logger.LogError(
                            $"[EventBus-Kafka] received invalid event name \"{message.EventName}\", expect \"{listener.Descriptor.EventName}\"");
                        continue;
                    }

                    Logger.LogDebug(
                        $"[EventBus-Kafka] received msg: {message?.ToJson()}.");

                    // 处理消息
                    listener.OnReceive(message, cancellationToken);
                    // 存储偏移,提交消息, see: https://docs.confluent.io/current/clients/dotnet.html
                    cunsumer.StoreOffset(consumerResult);
                }
            }, cancellationToken);
        }
    }
}