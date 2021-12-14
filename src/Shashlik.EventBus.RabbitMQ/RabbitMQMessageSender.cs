using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

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
            Logger = logger;
            MessageSerializer = messageSerializer;
            Channel = connection.GetChannel();
        }

        private IOptionsMonitor<EventBusRabbitMQOptions> Options { get; }
        private IModel Channel { get; }
        private ILogger<RabbitMQMessageSender> Logger { get; }
        private IMessageSerializer MessageSerializer { get; }

        public async Task SendAsync(MessageTransferModel message)
        {
            // 交换机定义,类型topic
            Channel.ExchangeDeclare(Options.CurrentValue.Exchange, "topic", true);
            var basicProperties = Channel.CreateBasicProperties();
            basicProperties.MessageId = message.MsgId;
            // 启用消息持久化
            basicProperties.Persistent = true;
            Channel.BasicPublish(Options.CurrentValue.Exchange, message.EventName, basicProperties,
                MessageSerializer.SerializeToBytes(message));
            // 等待消费端ack消息
            Channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(Options.CurrentValue.ConfirmTimeout));
            Logger.LogDebug($"[EventBus-RabbitMQ] send msg success: {message}");

            await Task.CompletedTask;
        }
    }
}