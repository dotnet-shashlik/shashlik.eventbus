using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shashlik.EventBus.RabbitMQ
{
    public static class RabbitMQExtensions
    {
        public static IEventBusBuilder AddRabbitMQ(this IEventBusBuilder builder,
            IConfigurationSection configurationSection)
        {
            builder.ServiceCollection.Configure<EventBusRabbitMQOptions>(configurationSection);

            return builder.AddRabbitMQ();
        }

        public static IEventBusBuilder AddRabbitMQ(this IEventBusBuilder builder,
            Action<EventBusRabbitMQOptions> action)
        {
            builder.ServiceCollection.Configure(action);

            return builder.AddRabbitMQ();
        }

        public static IEventBusBuilder AddRabbitMQ(this IEventBusBuilder builder)
        {
            builder.ServiceCollection.AddOptions<EventBusRabbitMQOptions>();
            builder.ServiceCollection.AddSingleton<IMessageSender, RabbitMQMessageSender>();
            builder.ServiceCollection.AddTransient<IMessageCunsumerRegistry, RabbitMQMessageCunsumerRegistry>();
            builder.ServiceCollection.AddSingleton<IRabbitMQConnection, DefaultRabbitMQConnection>();

            return builder;
        }
    }
}