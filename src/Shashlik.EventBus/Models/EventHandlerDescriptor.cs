#nullable disable
using System;

// ReSharper disable CheckNamespace

namespace Shashlik.EventBus
{
    /// <summary>
    /// 事件处理类描述信息
    /// </summary>
    public class EventHandlerDescriptor
    {
        /// <summary>
        /// 事件处理名称(NameRuler规则计算后)
        /// </summary>
        public string EventHandlerName { get; set; }

        /// <summary>
        /// 事件名称(NameRuler规则计算后)
        /// </summary>
        public string EventName { get; set; }

        // /// <summary>
        // /// 是否为延迟事件
        // /// </summary>
        // public bool IsDelay { get; set; }

        /// <summary>
        /// 事件类型
        /// </summary>
        public Type EventType { get; set; }

        /// <summary>
        /// 事件处理类名称,将同时注册为service和impl
        /// </summary>
        public Type EventHandlerType { get; set; }
    }
}