using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.Kafka
{
    /// <summary>
    /// 消息发送处理类
    /// </summary>
    public class RabbitMQMessageSender : IMessageSender
    {
        public Task Send(MessageTransferModel message)
        {
            throw new NotImplementedException();
        }
    }
}