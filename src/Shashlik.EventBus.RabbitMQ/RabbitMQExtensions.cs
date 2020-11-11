using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shashlik.EventBus.RabbitMQ
{
    public static class RabbitMQExtensions
    {
        public static IEventBusBuilder AddRabbitMQ(this IEventBusBuilder serviceCollection,
            IConfigurationSection configurationSection)
        {
            serviceCollection.Services.Configure<EventBusRabbitMQOptions>(configurationSection);

            return serviceCollection.AddRabbitMQ();
        }

        public static IEventBusBuilder AddRabbitMQ(this IEventBusBuilder serviceCollection,
            Action<EventBusRabbitMQOptions> action)
        {
            serviceCollection.Services.Configure(action);

            return serviceCollection.AddRabbitMQ();
        }

        public static IEventBusBuilder AddRabbitMQ(this IEventBusBuilder serviceCollection)
        {
            serviceCollection.Services.AddOptions<EventBusRabbitMQOptions>();
            serviceCollection.Services.AddSingleton<IMessageSender, RabbitMQMessageSender>();
            serviceCollection.Services.AddTransient<IEventSubscriber, RabbitMQEventSubscriber>();
            serviceCollection.Services.AddSingleton<IRabbitMQConnection, DefaultRabbitMQConnection>();

            return serviceCollection;
        }
    }
}