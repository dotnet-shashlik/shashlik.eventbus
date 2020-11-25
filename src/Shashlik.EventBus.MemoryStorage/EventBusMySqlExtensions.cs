using Microsoft.Extensions.DependencyInjection;

// ReSharper disable AssignNullToNotNullAttribute

namespace Shashlik.EventBus.MemoryStorage
{
    public static class MemoryStorageExtensions
    {
        public static IEventBusBuilder AddMySql(this IEventBusBuilder service)
        {
            service.Services.AddSingleton<IMessageStorage, MemoryMessageStorage>();
            service.Services.AddTransient<IMessageStorageInitializer, MemoryStorageInitializer>();

            return service;
        }
    }
}