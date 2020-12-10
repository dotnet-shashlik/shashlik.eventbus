#nullable disable
using System;
using System.Collections.Generic;

// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global

namespace Shashlik.EventBus
{
    /// <summary>
    /// 消息传输模型
    /// </summary>
    public class MessageTransferModel
    {
        /// <summary>
        /// 事件名称
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// 消息id
        /// </summary>
        public string MsgId { get; set; }

        /// <summary>
        /// 消息体, 事件内容序列化的内容
        /// </summary>
        public string MsgBody { get; set; }

        /// <summary>
        /// 附加数据
        /// </summary>
        public IDictionary<string, string> Items { get; set; }

        /// <summary>
        /// 发送时间
        /// </summary>
        public DateTimeOffset SendAt { get; set; }

        /// <summary>
        /// 延迟消费时间
        /// </summary>
        public DateTimeOffset? DelayAt { get; set; }

        ~MessageTransferModel()
        {
            //Console.WriteLine($"MessageTransferModel MsgId: {MsgId} disposed.");
        }
    }
}