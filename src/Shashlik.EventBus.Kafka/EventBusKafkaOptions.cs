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
            {"bootstrap.servers", "localhost"},
            {"enable.auto.offset.store", "false"},
            {"enable.auto.commit", "true"},
            {"auto.offset.reset", "earliest"},
        };

        public void AddOrUpdate(string key, string value)
        {
            if (key is null) throw new ArgumentNullException(nameof(key));
            if (value is null) throw new ArgumentNullException(nameof(value));
            Properties[key] = value;
        }

        public void AddOrUpdate(IEnumerable<KeyValuePair<string, string>> values)
        {
            if (values is null) throw new ArgumentNullException(nameof(values));
            values.ForEachItem(r => AddOrUpdate(r.Key, r.Value));
        }
    }
}