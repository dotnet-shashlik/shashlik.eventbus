using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.Kafka
{
    /// <summary>
    /// 消息发送处理类
    /// </summary>
    public class KafkaMessageSender : IMessageSender
    {
        public KafkaMessageSender(IKafkaConnection connection, IMessageSerializer messageSerializer,
            ILogger<KafkaMessageSender> logger)
        {
            Connection = connection;
            MessageSerializer = messageSerializer;
            Logger = logger;
        }

        private IKafkaConnection Connection { get; }
        private IMessageSerializer MessageSerializer { get; }
        private ILogger<KafkaMessageSender> Logger { get; }

        public async Task SendAsync(MessageTransferModel message)
        {
            // 从池借一个 producer,用完归还
            await using var lease = await Connection.GetProducer(message.EventName).ConfigureAwait(false);
            var producer = lease.Value
                ?? throw new EventBusException("[EventBus-Kafka] failed to rent producer from pool");

            var result = await producer.ProduceAsync(message.EventName, new Message<string, byte[]>
            {
                Key = message.MsgId,
                Value = MessageSerializer.SerializeToBytes(message)
            }).ConfigureAwait(false);

            if (result.Status is PersistenceStatus.Persisted or PersistenceStatus.PossiblyPersisted)
                Logger.LogDebug($"[EventBus-Kafka] send msg success: {message}");
            else
                throw new EventBusException(
                    $"[EventBus-Kafka] send msg fail, produce status \"{result.Status}\", message: {message}");
        }
    }
}
