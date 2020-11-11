using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.RabbitMQ
{
    /// <summary>
    /// 消息发送处理类
    /// </summary>
    public class RabbitMQMessageSender : IMessageSender
    {
        public RabbitMQMessageSender(IOptionsMonitor<EventBusRabbitMQOptions> options,
            IRabbitMQConnection connection, ILogger<RabbitMQMessageSender> logger, IMessageSerializer messageSerializer)
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

        public async Task Send(MessageTransferModel message)
        {
            Channel.ExchangeDeclare(Options.CurrentValue.Exchange, "topic", true);

            var basicProperties = Channel.CreateBasicProperties();
            basicProperties.MessageId = message.MsgId;
            basicProperties.Headers = new Dictionary<string, object>();

            if (message.DelayAt.HasValue)
            {
                basicProperties = Channel.CreateBasicProperties();
                var ex = (long) (message.DelayAt.Value - DateTimeOffset.Now).TotalMilliseconds;
                basicProperties.Expiration = ex.ToString();
            }

            Channel.BasicPublish(Options.CurrentValue.Exchange, message.EventName, basicProperties,
                Encoding.UTF8.GetBytes(MessageSerializer.Serialize(message))
            );

            Logger.LogDebug($"[EventBus-RabbitMQ] send msg success: {message.ToJson()}");

            await Task.CompletedTask;
        }
    }
}