using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public IEnumerable<EventHandlerDescriptor> FindAll()
        {
            if (_cache is null)
            {
                var types = ReflectionHelper.GetFinalSubTypes(typeof(IEventHandler<>));

                List<EventHandlerDescriptor> list = new();
                foreach (var typeInfo in types)
                {
                    var eventType = GetEventType(typeInfo);
                    list.Add(new EventHandlerDescriptor
                    {
                        EventHandlerName = EventHandlerNameRuler.GetName(typeInfo),
                        EventName = EventNameRuler.GetName(GetEventType(typeInfo)),
                        EventType = eventType,
                        EventHandlerType = typeInfo
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
    }
}