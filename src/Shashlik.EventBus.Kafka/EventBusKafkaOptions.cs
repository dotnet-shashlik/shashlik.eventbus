using System.Collections.Generic;

namespace Shashlik.EventBus.Kafka
{
    public class EventBusKafkaOptions
    {
        /// <summary>
        /// see: https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md
        /// </summary>
        public List<string[]> Properties { get; set; } = new List<string[]>();
    }
}