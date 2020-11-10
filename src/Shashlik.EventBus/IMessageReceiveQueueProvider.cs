using System.Collections.Generic;
using System.Threading;

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
        /// <param name="cancellationToken"></param>
        void Enqueue(MessageStorageModel message, IDictionary<string, string> items, EventHandlerDescriptor descriptor,
            CancellationToken cancellationToken);
    }
}