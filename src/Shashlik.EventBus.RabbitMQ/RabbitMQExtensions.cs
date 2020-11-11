using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shashlik.EventBus.RabbitMQ
{
    public static class RabbitMQExtensions
    {
        public static IServiceCollection AddRabbitMQ(this IServiceCollection serviceCollection,
            IConfigurationSection configurationSection)
        {
            serviceCollection.Configure<EventBusRabbitMQOptions>(configurationSection);

            return serviceCollection.AddRabbitMQ();
        }

        public static IServiceCollection AddRabbitMQ(this IServiceCollection serviceCollection,
            Action<EventBusRabbitMQOptions> action)
        {
            serviceCollection.Configure(action);

            return serviceCollection.AddRabbitMQ();
        }

        public static IServiceCollection AddRabbitMQ(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddOptions<EventBusRabbitMQOptions>();
            serviceCollection.AddSingleton<IMessageSender, RabbitMQMessageSender>();
            serviceCollection.AddTransient<IEventSubscriber, RabbitMQEventSubscriber>();
            serviceCollection.AddSingleton<IRabbitMQConnection, DefaultRabbitMQConnection>();

            return serviceCollection;
        }
    }
}