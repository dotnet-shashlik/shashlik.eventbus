using System;

// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable CheckNamespace

namespace Shashlik.EventBus
{
    /// <summary>
    /// 事件/事件处理名称定义
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class EventBusNameAttribute : Attribute
    {
        public EventBusNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}