using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

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
            var producer = Connection.GetProducer(message.EventName);

            var result = await producer.ProduceAsync(message.EventName, new Message<string, byte[]>
            {
                Key = message.MsgId,
                Value = Encoding.UTF8.GetBytes(MessageSerializer.Serialize(message))
            }).ConfigureAwait(false);

            if (result.Status == PersistenceStatus.Persisted || result.Status == PersistenceStatus.PossiblyPersisted)
                Logger.LogDebug($"[EventBus-Kafka] send msg success: {message}");
            else
                throw new EventBusException(
                    $"[EventBus-Kafka] send msg fail, produce status \"{result.Status}\", message: {message}");
        }
    }
}