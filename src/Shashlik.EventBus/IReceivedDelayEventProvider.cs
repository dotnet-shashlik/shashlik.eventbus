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
        /// <param name="message">存储消息内容</param>
        /// <param name="items">附加数据</param>
        /// <param name="descriptor">处理类描述器</param>
        /// <param name="cancellationToken"></param>
        void Enqueue(
            MessageStorageModel message,
            IDictionary<string, string> items,
            EventHandlerDescriptor descriptor,
            CancellationToken cancellationToken);
    }
}