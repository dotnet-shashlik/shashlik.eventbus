using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Shashlik.Utils.Extensions;
using Shashlik.Utils.Helpers;

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

        private IEnumerable<EventHandlerDescriptor>? _cache;

        public IEnumerable<EventHandlerDescriptor> LoadAll()
        {
            if (_cache is null)
            {
                var types = ReflectionHelper.GetFinalSubTypes(typeof(IEventHandler<>));

                List<EventHandlerDescriptor> list = new List<EventHandlerDescriptor>();
                foreach (var typeInfo in types)
                {
                    var eventType = GetEventType(typeInfo);
                    list.Add(new EventHandlerDescriptor
                    {
                        EventHandlerName = EventHandlerNameRuler.GetName(typeInfo),
                        EventName = EventNameRuler.GetName(GetEventType(typeInfo)),
                        IsDelay = eventType.IsSubTypeOrEqualsOf<IDelayEvent>(),
                        EventType = eventType,
                        EventHandlerType = typeInfo
                    });
                }

                _cache = list;
            }

            return _cache;
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