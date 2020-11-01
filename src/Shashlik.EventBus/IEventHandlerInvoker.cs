using System.Collections.Generic;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 事件处理类执行器
    /// </summary>
    public interface IEventHandlerInvoker
    {
        /// <summary>
        /// 事件处理执行
        /// </summary>
        /// <param name="messageStorageModel">消息存储模型</param>
        /// <param name="items">附加数据</param>
        /// <param name="eventHandlerDescriptor">事件处理描述器</param>
        void Invoke(MessageStorageModel messageStorageModel, IDictionary<string, string> items,
            EventHandlerDescriptor eventHandlerDescriptor);
    }
}