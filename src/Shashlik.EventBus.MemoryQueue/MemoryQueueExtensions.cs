using Microsoft.Extensions.DependencyInjection;

namespace Shashlik.EventBus.MemoryQueue
{
    public static class MemoryQueueExtensions
    {
        public static IEventBusBuilder AddMemoryQueue(this IEventBusBuilder eventBusBuilder)
        {
            eventBusBuilder.Services.AddSingleton<IMessageSender, MemoryMessageSender>();
            eventBusBuilder.Services.AddTransient<IEventSubscriber, MemoryEventSubscriber>();
            eventBusBuilder.Services.AddHostedService<MemoryQueueHostedService>();
            return eventBusBuilder;
        }
    }
}