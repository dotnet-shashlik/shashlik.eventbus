using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shashlik.Utils.Extensions;

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

        public async Task Send(MessageTransferModel message)
        {
            var producer = Connection.GetProducer();

            // 延迟消息的延迟时间写到header中
            var headers = new Headers();
            if (message.DelayAt.HasValue)
                headers.Add(EventBusConsts.DelayAtHeaderKey,
                    BitConverter.GetBytes(message.DelayAt.Value.GetLongDate()));

            var result = await producer.ProduceAsync(message.EventName, new Message<string, byte[]>
            {
                Headers = headers,
                Key = message.MsgId,
                Value = Encoding.UTF8.GetBytes(MessageSerializer.Serialize(message))
            });

            if (result.Status == PersistenceStatus.Persisted || result.Status == PersistenceStatus.PossiblyPersisted)
            {
                Logger.LogDebug($"[EventBus-RabbitMQ] send msg success: {message.ToJson()}");
                return;
            }

            throw new InvalidOperationException($"[EventBus-RabbitMQ] send msg fail: {message.ToJson()}");
        }
    }
}