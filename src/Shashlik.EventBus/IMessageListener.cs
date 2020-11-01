using System;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 消息监听执行器
    /// </summary>
    public interface IMessageListener
    {
        /// <summary>
        /// 处理类描述
        /// </summary>
        EventHandlerDescriptor Descriptor { get; }

        /// <summary>
        /// 消息接收处理
        /// </summary>
        /// <param name="messageTransferModel"></param>
        void Receive(MessageTransferModel messageTransferModel);
    }
}