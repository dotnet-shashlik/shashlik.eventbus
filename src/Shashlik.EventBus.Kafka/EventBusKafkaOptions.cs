using System;
using System.Collections.Generic;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.Kafka
{
    public class EventBusKafkaOptions
    {
        /// <summary>
        /// see: https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md
        /// </summary>
        public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>
        {
            { "bootstrap.servers", "localhost" },
            // 这几项配置不要覆盖,否则会影响消息的接收确认
            { "enable.auto.offset.store", "false" },
            { "enable.auto.commit", "true" },
            { "auto.offset.reset", "earliest" },
        };

        public void AddOrUpdate(string key, string value)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(value);
            Properties[key] = value;
        }

        public void AddOrUpdate(IEnumerable<KeyValuePair<string, string>> values)
        {
            ArgumentNullException.ThrowIfNull(values);
            values.ForEachItem(r => AddOrUpdate(r.Key, r.Value));
        }
    }
}