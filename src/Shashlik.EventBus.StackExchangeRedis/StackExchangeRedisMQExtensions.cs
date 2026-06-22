using System;
using Microsoft.Extensions.DependencyInjection;

namespace Shashlik.EventBus.StackExchangeRedis
{
    public static class StackExchangeRedisMQExtensions
    {
        /// <summary>
        /// add StackExchange.Redis mq services(redis stream)
        /// </summary>
        public static IEventBusBuilder AddStackExchangeRedisMQ(this IEventBusBuilder eventBusBuilder,
            Action<EventBusStackExchangeRedisOptions>? configure = null)
        {
            eventBusBuilder.Services.Configure(configure ?? (r => { }));
            eventBusBuilder.Services.AddSingleton<IMessageSender, StackExchangeRedisMQMessageSender>();
            eventBusBuilder.Services.AddSingleton<IEventSubscriber, StackExchangeRedisMQEventSubscriber>();
            return eventBusBuilder;
        }

        /// <summary>
        /// add StackExchange.Redis worker id services
        /// </summary>
        public static IEventBusBuilder AddStackExchangeRedisWorkerId(this IEventBusBuilder eventBusBuilder,
            Action<EventBusStackExchangeRedisOptions>? configure = null)
        {
            eventBusBuilder.Services.Configure(configure ?? (r => { }));
            eventBusBuilder.Services.AddSingleton<IIdGenerator, StackExchangeRedisWorkerIdGenerator>();
            return eventBusBuilder;
        }
    }
}
