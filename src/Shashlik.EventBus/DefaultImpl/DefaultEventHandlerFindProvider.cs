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

        public IEnumerable<EventHandlerDescriptor> LoadAll()
        {
            var types = ReflectHelper.GetFinalSubTypes(typeof(IEventHandler<>));

            foreach (var typeInfo in types)
            {
                var eventType = GetEventType(typeInfo);
                yield return new EventHandlerDescriptor
                {
                    EventHandlerName = EventHandlerNameRuler.GetName(typeInfo),
                    EventName = EventNameRuler.GetName(GetEventType(typeInfo)),
                    IsDelay = eventType.IsSubTypeOf<IDelayEvent>(),
                    EventType = eventType,
                    EventHandlerType = typeInfo
                };
            }
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