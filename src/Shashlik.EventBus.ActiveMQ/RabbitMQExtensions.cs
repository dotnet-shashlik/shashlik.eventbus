using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shashlik.EventBus.RabbitMQ
{
    public static class RabbitMQExtensions
    {
        /// <summary>
        /// add rabbit mq services
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="configurationSection"></param>
        /// <returns></returns>
        public static IEventBusBuilder AddRabbitMQ(this IEventBusBuilder eventBusBuilder,
            IConfigurationSection configurationSection)
        {
            eventBusBuilder.Services.Configure<EventBusRabbitMQOptions>(configurationSection);

            return eventBusBuilder.AddRabbitMQCore();
        }

        /// <summary>
        /// add rabbit mq services
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static IEventBusBuilder AddRabbitMQ(this IEventBusBuilder eventBusBuilder,
            Action<EventBusRabbitMQOptions> action)
        {
            eventBusBuilder.Services.Configure(action);

            return eventBusBuilder.AddRabbitMQCore();
        }

        /// <summary>
        /// add rabbit mq core services
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <returns></returns>
        public static IEventBusBuilder AddRabbitMQCore(this IEventBusBuilder eventBusBuilder)
        {
            eventBusBuilder.Services.AddOptions<EventBusRabbitMQOptions>();
            eventBusBuilder.Services.AddSingleton<IMessageSender, RabbitMQMessageSender>();
            eventBusBuilder.Services.AddSingleton<IEventSubscriber, RabbitMQEventSubscriber>();
            eventBusBuilder.Services.AddSingleton<IRabbitMQConnection, DefaultRabbitMQConnection>();

            return eventBusBuilder;
        }
    }
}