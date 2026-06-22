using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka.Admin;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.Kafka
{
    public class EventBusKafkaOptions
    {
        /// <summary>
        /// see: https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md
        /// </summary>
        public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>
        {
            { "bootstrap.servers", "localhost" }
        };

        internal IDictionary<string, string> GetProducerProperties()
        {
            var dic = new Dictionary<string, string>
            {
                // producer消息发送阻塞配置,默认值本身也是-1
                { "acks", "all" },
                // 幂等性
                { "enable.idempotence", "true" } 
            };
            var p = Properties.ToDictionary(r => r.Key, r => r.Value);
            dic.ForEachItem(r => p[r.Key] = r.Value);
            return p;
        }
        
        internal IDictionary<string, string> GetConsumeProperties()
        {
            var dic = new Dictionary<string, string>
            {
                // 这几项配置不要覆盖,否则会影响消息的接收确认
                { "enable.auto.offset.store", "false" },
                // 启用自动提交
                { "enable.auto.commit", "false" },
                // 未提交的偏移从头开始,防止消息丢失,默认是latest,但可能重复消费
                { "auto.offset.reset", "earliest" },
                { "max.poll.interval.ms", "600000" }
            };
            var p = Properties.ToDictionary(r => r.Key, r => r.Value);
            dic.ForEachItem(r => p[r.Key] = r.Value);
            return p;
        }

        /**
         * 创建新的topic时分区数量设置,默认-1
         */
        public int TopicNumPartitions { get; set; } = -1;

        /**
         * 创建新的topic时副本集数量,默认-1,需要考虑broker的数量
         */
        public short TopicReplicationFactor { get; set; } = -1;

        /// <summary>
        /// 消费池大小,默认4
        /// </summary>
        public int ConsumerPoolSize { get; set; } = 4;

        /**
         * 自定义配置topic创建,优先级高于<see cref="TopicNumPartitions"/>和<see cref="TopicReplicationFactor"/>
         */
        public Action<TopicSpecification>? ConfigTopic { get; set; }

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