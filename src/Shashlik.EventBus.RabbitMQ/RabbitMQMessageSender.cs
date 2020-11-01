using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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
            IRabbitMQConnection connection)
        {
            Options = options;
            Channel = connection.GetChannel();
        }

        private IOptionsMonitor<EventBusRabbitMQOptions> Options { get; }
        private IModel Channel { get; }

        public async Task Send(MessageTransferModel message)
        {
            Channel.ExchangeDeclare(Options.CurrentValue.Exchange, "topic", true);

            var basicProperties = Channel.CreateBasicProperties();
            basicProperties.MessageId = message.MsgId;
            basicProperties.Headers = new Dictionary<string, object>();

            // 附加数据放到header中传输
            if (!message.Items.IsNullOrEmpty())
                foreach (var keyValuePair in message.Items)
                    basicProperties.Headers.TryAdd(keyValuePair.Key, keyValuePair.Value);

            if (message.DelayAt.HasValue)
            {
                basicProperties = Channel.CreateBasicProperties();
                var ex = (long) (message.DelayAt.Value - DateTimeOffset.Now).TotalMilliseconds;
                //TODO: 延迟时间偏差计算
                basicProperties.Expiration = ex.ToString();
            }

            Channel.BasicPublish(Options.CurrentValue.Exchange, message.EventName, basicProperties,
                Encoding.UTF8.GetBytes(message.MsgBody)
            );

            await Task.CompletedTask;
        }
    }
}