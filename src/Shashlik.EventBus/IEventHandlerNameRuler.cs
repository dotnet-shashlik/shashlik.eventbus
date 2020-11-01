using System;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 事件处理类名称规则定义
    /// </summary>
    public interface IEventHandlerNameRuler
    {
        string GetName(Type eventHandlerType);
    }
}