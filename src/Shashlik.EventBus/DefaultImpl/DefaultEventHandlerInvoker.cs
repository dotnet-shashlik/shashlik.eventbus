using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Shashlik.EventBus.DefaultImpl
{
    public class DefaultEventHandlerInvoker : IEventHandlerInvoker
    {
        public DefaultEventHandlerInvoker(IMessageSerializer messageSerializer, IServiceScopeFactory serviceScopeFactory)
        {
            MessageSerializer = messageSerializer;
            ServiceScopeFactory = serviceScopeFactory;
        }

        private IServiceScopeFactory ServiceScopeFactory { get; }
        private IMessageSerializer MessageSerializer { get; }

        public void Invoke(MessageStorageModel messageStorageModel, IDictionary<string, string> items,
            EventHandlerDescriptor eventHandlerDescriptor)
        {
            using var scope = ServiceScopeFactory.CreateScope();
            var eventHandlerInstance = scope.ServiceProvider.GetService(eventHandlerDescriptor.EventHandlerType);
            if (eventHandlerInstance == null)
                throw new InvalidCastException(
                    $"[EventBus] can not find event handler of service type: {eventHandlerDescriptor.EventHandlerType}.");

            var method =
                eventHandlerDescriptor.EventHandlerType.GetMethod("Execute",
                    new Type[] {eventHandlerDescriptor.EventType, typeof(IDictionary<string, string>)});

            var eventBody =
                MessageSerializer.Deserialize(messageStorageModel.EventBody, eventHandlerDescriptor.EventType);

            var task = (Task) method!.Invoke(eventHandlerInstance, new object[] {eventBody, items});
            task.ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}