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
            var channel = await Connection.GetChannelAsync().ConfigureAwait(false);
            // 7.x: BasicProperties is a concrete class, not created from the channel.
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
            // RabbitMQ.Client 7.x removed WaitForConfirmsOrDie*. Publisher confirms are
            // configured per-channel via CreateChannelOptions and tracked automatically.
            // The publish above will throw on negative confirmations when tracking is on.
            Logger.LogDebug($"[EventBus-RabbitMQ] send msg success: {message}");
        }
    }
}
