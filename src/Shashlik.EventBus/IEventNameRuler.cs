using System;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 事件名称定义规则
    /// </summary>
    public interface IEventNameRuler
    {
        /// <summary>
        /// 获取事件名称
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <returns></returns>
        string GetName(Type eventType);
    }
}