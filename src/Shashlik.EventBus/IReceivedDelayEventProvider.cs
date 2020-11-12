using System.Collections.Generic;
using System.Threading;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 已接收的消息延迟事件处理器
    /// </summary>
    public interface IReceivedDelayEventProvider
    {
        /// <summary>
        /// 入队,进入待执行队列
        /// </summary>
        /// <param name="message"></param>
        /// <param name="items"></param>
        /// <param name="descriptor"></param>
        /// <param name="cancellationToken"></param>
        void Enqueue(
            MessageStorageModel message,
            IDictionary<string, string> items,
            EventHandlerDescriptor descriptor,
            CancellationToken cancellationToken);
    }
}