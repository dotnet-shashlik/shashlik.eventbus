using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.RabbitMQ
{
    public class RabbitMQMessageCunsumerRegistry : IMessageCunsumerRegistry
    {
        public RabbitMQMessageCunsumerRegistry(IOptionsMonitor<EventBusRabbitMQOptions> options,
            IRabbitMQConnection connection)
        {
            Options = options;
            Channel = connection.GetChannel();
        }

        private IOptionsMonitor<EventBusRabbitMQOptions> Options { get; }
        private IModel Channel { get; }

        public void Subscribe(IMessageListener listener)
        {
            // 注册基础通信交换机
            Channel.ExchangeDeclare(Options.CurrentValue.Exchange, "topic", true);
            // 注册死信交换机
            Channel.ExchangeDeclare(Options.CurrentValue.DeadExchange, "topic", true);
            // 如果是延迟队列,先定义死信交换机
            Channel.QueueDeclare(listener.Descriptor.EventHandlerName, true, false, false);

            // 延迟队列需要配置死信队列
            if (listener.Descriptor.IsDelay)
            {
                // 配置延迟队列
                var delayQueue = $"{listener.Descriptor.EventHandlerName}.DELAY";
                var map = new Dictionary<string, object>
                {
                    {"x-dead-letter-exchange", Options.CurrentValue.DeadExchange},
                    {"x-dead-letter-routing-key", listener.Descriptor.EventName}
                };
                // 如果是延迟队列,先定义死信交换机
                Channel.QueueDeclare(delayQueue, true, false, false, map);

                // 如果是延迟任务,队列绑定到死信交换机
                Channel.QueueBind(listener.Descriptor.EventHandlerName, Options.CurrentValue.DeadExchange,
                    listener.Descriptor.EventName);
                // 如果是延迟任务,延迟队列绑定正常的交换机
                Channel.QueueBind(delayQueue, Options.CurrentValue.Exchange, listener.Descriptor.EventName);
            }
            else
            {
                Channel.QueueBind(listener.Descriptor.EventHandlerName, Options.CurrentValue.Exchange,
                    listener.Descriptor.EventName);
            }

            var consumer = new EventingBasicConsumer(Channel);
            consumer.Received += (sender, e) =>
            {
                var message = new MessageTransferModel
                {
                    EventName = listener.Descriptor.EventName,
                    MsgId = e.BasicProperties.MessageId,
                    MsgBody = Encoding.UTF8.GetString(e.Body),
                    Items = e.BasicProperties.Headers
                        .ToDictionary(r => r.Key, r =>
                        {
                            return Encoding.UTF8.GetString((byte[])r.Value);
                        }),
                    //SendAt = e.BasicProperties.Headers.GetOrDefault(EventBusConsts.SendAtHeaderKey)
                    //    .ParseTo<DateTimeOffset>(),
                    //DelayAt = e.BasicProperties.Headers.GetOrDefault(EventBusConsts.DelayAtHeaderKey)
                    //    .ParseTo<DateTimeOffset?>()
                };

                message.SendAt = message.Items[EventBusConsts.SendAtHeaderKey].ParseTo<DateTimeOffset>();
                message.DelayAt = message.Items.GetOrDefault(EventBusConsts.DelayAtHeaderKey).ParseTo<DateTimeOffset?>();

                listener.Receive(message);
                // 一定要在消息接收ok后才确认ack
                Channel.BasicAck(e.DeliveryTag, false);
            };

            Channel.BasicConsume(listener.Descriptor.EventHandlerName, false, consumer);
        }
    }
}