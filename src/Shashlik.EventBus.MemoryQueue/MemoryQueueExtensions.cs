using Microsoft.Extensions.DependencyInjection;

namespace Shashlik.EventBus.MemoryQueue
{
    public static class MemoryQueueExtensions
    {
        public static IEventBusBuilder AddMemoryQueue(this IEventBusBuilder eventBusBuilder)
        {
            eventBusBuilder.Services.AddSingleton<IMessageSender, MemoryMessageSender>();
            eventBusBuilder.Services.AddSingleton<IEventSubscriber, MemoryEventSubscriber>();
            return eventBusBuilder;
        }
    }
}