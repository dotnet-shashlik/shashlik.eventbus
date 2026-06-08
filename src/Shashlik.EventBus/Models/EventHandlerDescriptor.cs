#nullable disable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        /// <summary>
        /// 事件类型
        /// </summary>
        public Type EventType { get; set; }

        /// <summary>
        /// 事件处理类类型,将同时注册为service和impl
        /// </summary>
        public Type EventHandlerType { get; set; }

        /// <summary>
        /// 已编译的 Execute 委托(在 <see cref="DefaultImpl.DefaultEventHandlerFindProvider"/>
        /// 创建 descriptor 时一次编译,运行时直接调用,避免每条消息走反射 + TargetInvocationException 包装)。
        /// </summary>
        internal Func<object, IDictionary<string, string>, Task> ExecuteDelegate { get; set; }
    }
}