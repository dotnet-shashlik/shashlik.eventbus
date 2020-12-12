using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Shashlik.EventBus.DefaultImpl
{
    public class DefaultEventHandlerInvoker : IEventHandlerInvoker
    {
        public DefaultEventHandlerInvoker(
            IMessageSerializer messageSerializer,
            IServiceScopeFactory serviceScopeFactory)
        {
            MessageSerializer = messageSerializer;
            ServiceScopeFactory = serviceScopeFactory;
        }

        private IServiceScopeFactory ServiceScopeFactory { get; }
        private IMessageSerializer MessageSerializer { get; }

        public async Task InvokeAsync(MessageStorageModel messageStorageModel, IDictionary<string, string> items,
            EventHandlerDescriptor eventHandlerDescriptor)
        {
            using var scope = ServiceScopeFactory.CreateScope();
            var eventHandlerInstance =
                scope.ServiceProvider.GetRequiredService(eventHandlerDescriptor.EventHandlerType);
            if (messageStorageModel.EventBody is null)
                throw new InvalidCastException($"[EventBus] event body content is null, msgId: {messageStorageModel.MsgId}");
            var eventBody =
                MessageSerializer.Deserialize(messageStorageModel.EventBody, eventHandlerDescriptor.EventType);
            if (eventBody is null)
                throw new InvalidCastException($"[EventBus] event body content deserialize to null, msgId: {messageStorageModel.MsgId}");

            var method =
                eventHandlerDescriptor.EventHandlerType.GetMethod("Execute",
                    new Type[] {eventHandlerDescriptor.EventType, typeof(IDictionary<string, string>)});

            var task = (Task) method!.Invoke(eventHandlerInstance, new object[] {eventBody, items});
            await task.ConfigureAwait(false);
        }
    }
}