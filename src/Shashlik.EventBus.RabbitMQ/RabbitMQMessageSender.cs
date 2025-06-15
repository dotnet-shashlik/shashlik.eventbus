using System;
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
            var basicProperties = new BasicProperties();
            basicProperties.MessageId = message.MsgId;
            // 启用消息持久化
            basicProperties.Persistent = true;
            try
            {
                await channel.BasicPublishAsync(Options.CurrentValue.Exchange, message.EventName, true, basicProperties,
                    MessageSerializer.SerializeToBytes(message));
                Logger.LogDebug($"[EventBus-RabbitMQ] send msg success: {message}");

            }
            catch (Exception e)
            {
                Logger.LogError(e,$"[EventBus-RabbitMQ] send msg success: {message}");
                throw;
            }
            await Task.CompletedTask;
        }
    }
}