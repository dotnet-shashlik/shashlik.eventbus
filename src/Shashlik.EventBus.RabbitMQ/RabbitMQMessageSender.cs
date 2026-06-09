using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.RabbitMQ
{
    /// <summary>
    /// 消息发送处理类
    /// </summary>
    public class RabbitMQMessageSender : IMessageSender
    {
        public RabbitMQMessageSender(
            IOptionsMonitor<EventBusRabbitMQOptions> options,
            IRabbitMQConnection connection,
            ILogger<RabbitMQMessageSender> logger,
            IMessageSerializer messageSerializer)
        {
            Options = options;
            Connection = connection;
            Logger = logger;
            MessageSerializer = messageSerializer;
        }

        private IOptionsMonitor<EventBusRabbitMQOptions> Options { get; }
        private ILogger<RabbitMQMessageSender> Logger { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IRabbitMQConnection Connection { get; }

        public async Task SendAsync(MessageTransferModel message)
        {
            // 7.x: 通道从池借出,using 离开作用域时自动归还
            // (channel 已关闭则直接 dispose,不归还)。
            await using var lease = await Connection.GetChannelAsync().ConfigureAwait(false);
            var channel = lease.Value;
            if (channel is null)
                throw new EventBusException("[EventBus-RabbitMQ] failed to rent channel from pool");

            var basicProperties = new BasicProperties
            {
                MessageId = message.MsgId,
                Persistent = true
            };
            await channel.BasicPublishAsync(
                Options.CurrentValue.Exchange,
                message.EventName,
                true,
                basicProperties,
                MessageSerializer.SerializeToBytes(message)).ConfigureAwait(false);
            Logger.LogDebug($"[EventBus-RabbitMQ] send msg success: {message}");
        }
    }
}
