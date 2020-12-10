using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.Utils.Extensions;

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
            if (configurationSection == null) throw new ArgumentNullException(nameof(configurationSection));
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
            if (action == null) throw new ArgumentNullException(nameof(action));
            eventBusBuilder.Services.Configure(action);
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
            return eventBusBuilder.AddKafka(r => { r.Properties.Add(new[] {"bootstrap.servers", server}); });
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

        /// <summary>
        /// convert to List to Dictionary
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        /// <exception cref="InvalidCastException"></exception>
        internal static IDictionary<string, string> ConvertToDictionary(this List<string[]> list)
        {
            var dic = list.ToDictionary(r =>
            {
                if (r.IsNullOrEmpty() || r.Length != 2)
                    throw new InvalidCastException(
                        $"[EventBus-Kafka] kafka configuration must be two element, e.g. \"['allow.auto.create.topics', 'true']\"");
                return r[0];
            }, r => r[1]);

            // // 允许自动创建topic
            // 自行配置
            // dic["allow.auto.create.topics"] = "true";
            // 禁止自动保存offset
            dic["enable.auto.offset.store"] = "false";
            // 启用自动提交
            dic["enable.auto.commit"] = "true";
            dic["auto.offset.reset"] = "earliest";

            return dic;
        }
    }
}