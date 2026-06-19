using System;
using Microsoft.Extensions.DependencyInjection;

namespace Shashlik.EventBus.Redis
{
    public static class RedisMQExtensions
    {
        /// <summary>
        /// add redis mq services(redis stream)
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IEventBusBuilder AddRedisMQ(this IEventBusBuilder eventBusBuilder,
            Action<EventBusRedisMQOptions>? configure = null)
        {
            eventBusBuilder.Services.Configure(configure ?? (r => { }));
            eventBusBuilder.Services.AddSingleton<IMessageSender, RedisMQMessageSender>();
            eventBusBuilder.Services.AddSingleton<IEventSubscriber, RedisMQEventSubscriber>();
            return eventBusBuilder;
        }

        /// <summary>
        /// add redis worker id services
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IEventBusBuilder AddRedisWorkerId(this IEventBusBuilder eventBusBuilder,
            Action<EventBusRedisWorkerIdOptions>? configure = null)
        {
            eventBusBuilder.Services.Configure(configure ?? (r => { }));
            eventBusBuilder.Services.AddSingleton<IIdGenerator, RedisWorkerIdGenerator>();
            return eventBusBuilder;
        }
    }
}