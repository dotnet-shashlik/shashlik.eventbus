using System.Collections.Generic;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 消息接收处理队列
    /// </summary>
    public interface IMessageReceiveQueueProvider
    {
        /// <summary>
        /// 入队
        /// </summary>
        /// <param name="message"></param>
        /// <param name="items"></param>
        /// <param name="descriptor"></param>
        void Enqueue(MessageStorageModel message, IDictionary<string, string> items, EventHandlerDescriptor descriptor);
    }
}