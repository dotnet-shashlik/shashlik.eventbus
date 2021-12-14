using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shashlik.EventBus.Kafka
{
    public static class KafkaExtensions
    {
        /// <summary>
        /// add kafka services, properties see: https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="configurationSection"></param>
        /// <returns></returns>
        public static IEventBusBuilder AddKafka(this IEventBusBuilder eventBusBuilder,
            IConfigurationSection configurationSection)
        {
            if (configurationSection is null) throw new ArgumentNullException(nameof(configurationSection));
            eventBusBuilder.Services.Configure<EventBusKafkaOptions>(configurationSection);
            return eventBusBuilder.AddKafkaCore();
        }

        /// <summary>
        /// add kafka services, properties see: https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static IEventBusBuilder AddKafka(this IEventBusBuilder eventBusBuilder,
            Action<EventBusKafkaOptions> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));
            eventBusBuilder.Services.Configure(action);
            return eventBusBuilder.AddKafkaCore();
        }

        /// <summary>
        /// add kafka services, properties see: https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="kafkaConfigs">自定义kafka配置</param>
        /// <returns></returns>
        public static IEventBusBuilder AddKafka(this IEventBusBuilder eventBusBuilder,
            IDictionary<string, string> kafkaConfigs)
        {
            if (eventBusBuilder == null) throw new ArgumentNullException(nameof(eventBusBuilder));
            if (kafkaConfigs == null) throw new ArgumentNullException(nameof(kafkaConfigs));
            eventBusBuilder.Services.Configure<EventBusKafkaOptions>(r => r.AddOrUpdate(kafkaConfigs));
            return eventBusBuilder.AddKafkaCore();
        }

        /// <summary>
        /// add kafka services
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="server">kafka bootstrap server</param>
        /// <returns></returns>
        public static IEventBusBuilder AddKafka(this IEventBusBuilder eventBusBuilder, string server)
        {
            if (string.IsNullOrWhiteSpace(server))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(server));
            return eventBusBuilder.AddKafka(r => r.AddOrUpdate("bootstrap.servers", server));
        }

        /// <summary>
        /// add kafka core service
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <returns></returns>
        public static IEventBusBuilder AddKafkaCore(this IEventBusBuilder eventBusBuilder)
        {
            eventBusBuilder.Services.AddOptions<EventBusKafkaOptions>();
            eventBusBuilder.Services.AddSingleton<IMessageSender, KafkaMessageSender>();
            eventBusBuilder.Services.AddSingleton<IEventSubscriber, KafkaEventSubscriber>();
            eventBusBuilder.Services.AddSingleton<IKafkaConnection, DefaultKafkaConnection>();

            return eventBusBuilder;
        }
    }
}