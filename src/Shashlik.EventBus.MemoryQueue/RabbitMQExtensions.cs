using Microsoft.Extensions.DependencyInjection;

namespace Shashlik.EventBus.MemoryQueue
{
    public static class RabbitMQExtensions
    {
        public static IEventBusBuilder AddMemoryQueue(this IEventBusBuilder serviceCollection)
        {
            serviceCollection.Services.AddSingleton<IMessageSender, MemoryMessageSender>();
            serviceCollection.Services.AddTransient<IEventSubscriber, MemoryEventSubscriber>();
            return serviceCollection;
        }
    }
}