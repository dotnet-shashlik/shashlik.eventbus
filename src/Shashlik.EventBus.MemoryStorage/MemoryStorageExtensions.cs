using Microsoft.Extensions.DependencyInjection;

namespace Shashlik.EventBus.MemoryStorage
{
    public static class MemoryStorageExtensions
    {
        public static IEventBusBuilder AddMemoryStorage(this IEventBusBuilder service)
        {
            service.Services.AddSingleton<IMessageStorage, MemoryMessageStorage>();
            service.Services.AddTransient<IMessageStorageInitializer, MemoryStorageInitializer>();

            return service;
        }
    }
}