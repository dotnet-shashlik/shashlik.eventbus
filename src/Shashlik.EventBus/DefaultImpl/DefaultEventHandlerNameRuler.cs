using System;
using System.Reflection;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.DefaultImpl
{
    public class DefaultEventHandlerNameRuler : IEventHandlerNameRuler
    {
        public DefaultEventHandlerNameRuler(IOptions<EventBusOptions> options)
        {
            Options = options;
        }

        private IOptions<EventBusOptions> Options { get; }

        public string GetName(Type eventType)
        {
            var name = eventType.GetCustomAttribute<EventBusNameAttribute>()?.Name;
            name ??= eventType.Name;

            return $"{name}.{Options.Value.Environment}";
        }
    }
}