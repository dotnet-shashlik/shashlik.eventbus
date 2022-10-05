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
        /// <param name="action"></param>
        /// <returns></returns>
        public static IEventBusBuilder AddRedisMQ(this IEventBusBuilder eventBusBuilder,
            Action<EventBusRedisMQOptions>? action = null)
        {
            eventBusBuilder.Services.Configure(action ?? (r => { }));
            eventBusBuilder.Services.AddSingleton<IMessageSender, RedisMQMessageSender>();
            eventBusBuilder.Services.AddSingleton<IEventSubscriber, RedisMQEventSubscriber>();
            return eventBusBuilder;
        }
    }
}