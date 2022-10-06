﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shashlik.EventBus.Utils;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

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
            var channel = Connection.GetChannel();
            channel.ConfirmSelect();
            // 交换机定义,类型topic
            channel.ExchangeDeclare(Options.CurrentValue.Exchange, "topic", true);
            var basicProperties = channel.CreateBasicProperties();
            basicProperties.MessageId = message.MsgId;
            // 启用消息持久化
            basicProperties.Persistent = true;
            channel.BasicPublish(Options.CurrentValue.Exchange, message.EventName, basicProperties,
                MessageSerializer.SerializeToBytes(message));
            // 等待消费端ack消息
            channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(Options.CurrentValue.ConfirmTimeout));
            Logger.LogDebug($"[EventBus-RabbitMQ] send msg success: {message}");

            await Task.CompletedTask;
        }
    }
}