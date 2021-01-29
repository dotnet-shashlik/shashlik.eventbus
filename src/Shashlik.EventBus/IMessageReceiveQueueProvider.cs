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
        /// <param name="message">消息存储模型</param>
        /// <param name="additionalItems">附加数据</param>
        /// <param name="descriptor">事件处理类描述器</param>
        /// <param name="cancellationToken"></param>
        void Enqueue(MessageStorageModel message, IDictionary<string, string> additionalItems, EventHandlerDescriptor descriptor,
            CancellationToken cancellationToken);
    }
}