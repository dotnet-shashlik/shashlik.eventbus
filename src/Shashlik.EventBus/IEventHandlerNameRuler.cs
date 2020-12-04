using System;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 事件处理类名称规则定义
    /// </summary>
    public interface IEventHandlerNameRuler
    {
        /// <summary>
        /// 获取事件处理类名称
        /// </summary>
        /// <param name="eventHandlerType">事件处理类类型</param>
        /// <returns></returns>
        string GetName(Type eventHandlerType);
    }
}