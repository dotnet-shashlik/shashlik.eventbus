using System;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.Utils.Extensions;

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

            // 重置一些默认值
            serviceCollection.Configure<EventBusKafkaOptions>(r =>
            {
                r.Base.CopyTo(r.Producer);
                r.Base.CopyTo(r.Consumer);

                // see: https://docs.confluent.io/current/clients/dotnet.html
                r.Consumer.EnableAutoOffsetStore = false;
                r.Consumer.EnableAutoCommit = true;
                r.Consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
            });
            serviceCollection.AddSingleton<IMessageSender, KafkaMessageSender>();
            serviceCollection.AddTransient<IEventSubscriber, KafkaEventSubscriber>();
            serviceCollection.AddSingleton<IKafkaConnection, DefaultKafkaConnection>();

            return serviceCollection;
        }
    }
}