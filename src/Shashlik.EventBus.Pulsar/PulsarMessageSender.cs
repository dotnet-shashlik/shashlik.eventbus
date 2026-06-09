using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Pulsar.Client;
using Shashlik.EventBus.Pulsar;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.Pulsar
{
    /// <summary>
    /// 消息发送处理类
    /// </summary>
    public class PulsarMessageSender : IMessageSender
    {
        public PulsarMessageSender(
            IPulsarConnection connection,
            ILogger<PulsarMessageSender> logger,
            IMessageSerializer messageSerializer)
        {
            Connection = connection;
            Logger = logger;
            MessageSerializer = messageSerializer;
        }

        private ILogger<PulsarMessageSender> Logger { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IPulsarConnection Connection { get; }

        public async Task SendAsync(MessageTransferModel message)
        {
            // 从池借一个 producer,用完归还
            await using var lease = await Connection.GetProducer(message.EventName).ConfigureAwait(false);
            var producer = lease.Value
                ?? throw new EventBusException("[EventBus-Pulsar] failed to rent producer from pool");

            try
            {
                var messageBuilder = producer.NewMessage(MessageSerializer.SerializeToBytes(message));
                var result = await producer.SendAsync(messageBuilder).ConfigureAwait(false);
                if (result?.Type != null)
                {
                    Logger.LogDebug($"[EventBus-Pulsar] send msg success: {message}");
                }
            }
            catch (Exception e)
            {
                // 标记 producer 已坏,池在归还时丢弃
                lease.IsValid = false;
                throw new EventBusException(
                    $"[EventBus-Pulsar] send msg fail,  message: {message}", e);
            }
        }
    }
}
