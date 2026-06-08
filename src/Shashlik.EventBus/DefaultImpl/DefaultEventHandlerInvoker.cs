using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable RedundantExplicitArrayCreation

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
            if (messageStorageModel.EventBody is null)
                throw new InvalidCastException(
                    $"[EventBus] event body content is null, msgId: {messageStorageModel.MsgId}");

            using var scope = ServiceScopeFactory.CreateScope();
            var eventHandlerInstance =
                scope.ServiceProvider.GetRequiredService(eventHandlerDescriptor.EventHandlerType);

            if (eventHandlerDescriptor.ExecuteDelegate is not null)
            {
                // 编译好的委托,快路径
                await eventHandlerDescriptor.ExecuteDelegate(eventHandlerInstance!, items)
                    .ConfigureAwait(false);
                return;
            }

            // 回退路径(理论上不会走到,FindAll 阶段就编好了):反射 + 解包 TargetInvocationException
            var method = eventHandlerDescriptor.EventHandlerType.GetMethod("Execute",
                new[] { eventHandlerDescriptor.EventType, typeof(IDictionary<string, string>) });
            if (method is null)
                throw new InvalidOperationException(
                    $"[EventBus] no Execute({eventHandlerDescriptor.EventType}, IDictionary<string,string>) found on {eventHandlerDescriptor.EventHandlerType}");

            try
            {
                var task = (Task)method.Invoke(eventHandlerInstance, new object[] { items })!;
                await task.ConfigureAwait(false);
            }
            catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }
    }
}
