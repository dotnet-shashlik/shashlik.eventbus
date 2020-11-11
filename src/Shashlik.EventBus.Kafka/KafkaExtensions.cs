using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shashlik.EventBus.Kafka
{
    public static class KafkaExtensions
    {
        public static IServiceCollection AddKafka(this IServiceCollection serviceCollection,
            IConfigurationSection configurationSection)
        {
            serviceCollection.Configure<EventBusKafkaOptions>(configurationSection);
            return serviceCollection.AddKafka();
        }

        public static IServiceCollection AddKafka(this IServiceCollection serviceCollection,
            Action<EventBusKafkaOptions> action)
        {
            serviceCollection.Configure(action);
            return serviceCollection.AddKafka();
        }

        public static IServiceCollection AddKafka(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddOptions<EventBusKafkaOptions>();
            serviceCollection.AddSingleton<IMessageSender, KafkaMessageSender>();
            serviceCollection.AddTransient<IEventSubscriber, KafkaEventSubscriber>();
            serviceCollection.AddSingleton<IKafkaConnection, DefaultKafkaConnection>();

            return serviceCollection;
        }
    }
}