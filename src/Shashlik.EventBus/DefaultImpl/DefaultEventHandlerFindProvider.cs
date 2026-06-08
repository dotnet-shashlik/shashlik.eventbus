using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.DefaultImpl
{
    public class DefaultEventHandlerFindProvider : IEventHandlerFindProvider
    {
        public DefaultEventHandlerFindProvider(IEventNameRuler eventNameRuler,
            IEventHandlerNameRuler eventHandlerNameRuler)
        {
            EventNameRuler = eventNameRuler;
            EventHandlerNameRuler = eventHandlerNameRuler;
        }

        private IEventNameRuler EventNameRuler { get; }
        private IEventHandlerNameRuler EventHandlerNameRuler { get; }

        private static IDictionary<string, EventHandlerDescriptor>? _cache;
        private static readonly object CacheLock = new();

        public IEnumerable<EventHandlerDescriptor> FindAll()
        {
            if (_cache is not null) return _cache.Values;
            lock (CacheLock)
            {
                if (_cache is not null) return _cache.Values;

                var types = ReflectionHelper.GetFinalSubTypes(typeof(IEventHandler<>));

                List<EventHandlerDescriptor> list = new();
                foreach (var typeInfo in types)
                {
                    var eventType = GetEventType(typeInfo);
                    list.Add(new EventHandlerDescriptor
                    {
                        EventHandlerName = EventHandlerNameRuler.GetName(typeInfo),
                        EventName = EventNameRuler.GetName(eventType),
                        EventType = eventType,
                        EventHandlerType = typeInfo,
                        ExecuteDelegate = BuildExecuteDelegate(typeInfo, eventType)
                    });
                }

                _cache = list.ToDictionary(r => r.EventHandlerName, r => r);
            }

            return _cache.Values;
        }

        public EventHandlerDescriptor? GetByName(string eventHandlerName)
        {
            if (_cache is null)
                FindAll();

            return _cache!.GetOrDefault(eventHandlerName);
        }

        private static Type GetEventType(Type type)
        {
            return type.GetTypeInfo()
                .ImplementedInterfaces
                .Single(r => r.IsGenericType && r.GetGenericTypeDefinition() == typeof(IEventHandler<>))
                .GetGenericArguments()
                .Single();
        }

        /// <summary>
        /// 编译 IEventHandler&lt;T&gt;.Execute(T, IDictionary) 为强类型委托,
        /// 避免运行时 method.Invoke + TargetInvocationException 包装。
        /// 失败时返回 null,调用方会回退到反射路径(并解包 TargetInvocationException)。
        /// </summary>
        private static Func<object, IDictionary<string, string>, Task>? BuildExecuteDelegate(Type handlerType,
            Type eventType)
        {
            try
            {
                var method = handlerType.GetMethod("Execute",
                    new[] { eventType, typeof(IDictionary<string, string>) });
                if (method is null)
                    return null;

                // handlerInstance.Execute((T)@event, items) 的运行时适配
                return (instance, @event) =>
                {
                    try
                    {
                        var result = method.Invoke(instance, new[] { @event, (IDictionary<string, string>?)null });
                        return (Task)result!;
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException is not null)
                    {
                        // 解包反射异常,暴露真实堆栈
                        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException)
                            .Throw();
                        throw; // unreachable
                    }
                };
            }
            catch
            {
                return null;
            }
        }
    }
}