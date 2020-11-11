using System;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.Kafka
{
    public static class KafkaExtensions
    {
        public static IEventBusBuilder AddKafka(this IEventBusBuilder serviceCollection,
            IConfigurationSection configurationSection)
        {
            serviceCollection.Services.Configure<EventBusKafkaOptions>(configurationSection);
            return serviceCollection.AddKafka();
        }

        public static IEventBusBuilder AddKafka(this IEventBusBuilder serviceCollection,
            Action<EventBusKafkaOptions> action)
        {
            serviceCollection.Services.Configure(action);
            return serviceCollection.AddKafka();
        }

        public static IEventBusBuilder AddKafka(this IEventBusBuilder serviceCollection)
        {
            serviceCollection.Services.AddOptions<EventBusKafkaOptions>();

            // 重置一些默认值
            serviceCollection.Services.Configure<EventBusKafkaOptions>(r =>
            {
                r.Base.CopyTo(r.Producer);
                r.Base.CopyTo(r.Consumer);

                // see: https://docs.confluent.io/current/clients/dotnet.html
                //r.Consumer.EnableAutoOffsetStore = false;
                r.Consumer.EnableAutoCommit = false;
                r.Consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
            });
            serviceCollection.Services.AddSingleton<IMessageSender, KafkaMessageSender>();
            serviceCollection.Services.AddTransient<IEventSubscriber, KafkaEventSubscriber>();
            serviceCollection.Services.AddSingleton<IKafkaConnection, DefaultKafkaConnection>();

            return serviceCollection;
        }
    }
}