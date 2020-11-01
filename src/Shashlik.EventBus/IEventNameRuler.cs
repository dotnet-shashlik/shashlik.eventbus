using System;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 事件名称定义规则
    /// </summary>
    public interface IEventNameRuler
    {
        string GetName(Type eventType);
    }
}