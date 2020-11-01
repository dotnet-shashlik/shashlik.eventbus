using System.Collections.Generic;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 事件处理类查找器
    /// </summary>
    public interface IEventHandlerFindProvider
    {
        /// <summary>
        /// 加载系统所有的事件处理类
        /// </summary>
        /// <returns></returns>
        IEnumerable<EventHandlerDescriptor> LoadAll();
    }
}