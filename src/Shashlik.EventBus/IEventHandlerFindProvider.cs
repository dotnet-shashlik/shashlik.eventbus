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
        IEnumerable<EventHandlerDescriptor> FindAll();

        /// <summary>
        /// 根据事件处理名称获取描述器
        /// </summary>
        /// <param name="eventHandlerName">事件处理名称</param>
        /// <returns></returns>
        EventHandlerDescriptor? GetByName(string eventHandlerName);
    }
}