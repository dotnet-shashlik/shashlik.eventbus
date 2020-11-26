using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
            serviceCollection.Services.AddSingleton<IMessageSender, KafkaMessageSender>();
            serviceCollection.Services.AddTransient<IEventSubscriber, KafkaEventSubscriber>();
            serviceCollection.Services.AddSingleton<IKafkaConnection, DefaultKafkaConnection>();

            return serviceCollection;
        }

        public static IDictionary<string, string> ConvertToDictionary(this List<string[]> list)
        {
            var dic = list.ToDictionary(r =>
            {
                if (r.IsNullOrEmpty() || r.Length != 2)
                    throw new InvalidCastException(
                        $"[EventBus-Kafka] kafka configuration must be two item, like \"['allow.auto.create.topics', 'true']\"");
                return r[0];
            }, r => r[1]);

            // 允许自动创建topic
            dic["allow.auto.create.topics"] = "true";
            // 禁止自动保存offset
            dic["enable.auto.offset.store"] = "false";
            // 启用自动提交
            dic["enable.auto.commit"] = "true";
            dic["auto.offset.reset"] = "earliest";

            return dic;
        }
    }
}