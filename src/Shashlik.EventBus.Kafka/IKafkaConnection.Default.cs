using System;
using System.Collections.Concurrent;
using System.Data.SqlTypes;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.Kafka
{
    public class DefaultKafkaConnection : IKafkaConnection
    {
        public DefaultKafkaConnection(IOptionsMonitor<EventBusKafkaOptions> options)
        {
            Options = options;
        }

        private IOptionsMonitor<EventBusKafkaOptions> Options { get; }
        private ConcurrentDictionary<string, IProducer<string, byte[]>> Producers { get; } = new();
        private ConcurrentDictionary<string, IConsumer<string, byte[]>> Consumers { get; } = new();
        private ConcurrentBag<string> ExistsTopics { get; } = new();

        private async Task InitTopic(string topic)
        {
            if (ExistsTopics.Contains(topic))
                return;
            try
            {
                var dic = Options.CurrentValue.Properties;
                var config = new AdminClientConfig(dic);
                using var adminClient = new AdminClientBuilder(config).Build();
                await adminClient.CreateTopicsAsync(new[] { NewTopicSpecification(topic) })
                    .ConfigureAwait(false);
                ExistsTopics.Add(topic);
            }
            catch (CreateTopicsException ex)
            {
                if (!ex.Message.Contains("exists", StringComparison.OrdinalIgnoreCase))
                    throw;
                ExistsTopics.Add(topic);
            }
        }

        private TopicSpecification NewTopicSpecification(string name)
        {
            var topicSpecification = new TopicSpecification
            {
                NumPartitions = Options.CurrentValue.TopicNumPartitions,
                ReplicationFactor = Options.CurrentValue.TopicReplicationFactor
            };
            Options.CurrentValue.ConfigTopic?.Invoke(topicSpecification);
            topicSpecification.Name = name;
            return topicSpecification;
        }

        public IProducer<string, byte[]> GetProducer(string topic)
        {
            InitTopic(topic).ConfigureAwait(false).GetAwaiter().GetResult();
            var producerBuilder = new ProducerBuilder<string, byte[]>(Options.CurrentValue.Properties);
            return Producers.GetOrAdd(topic, _ =>
                producerBuilder.Build()
            );
        }

        public IConsumer<string, byte[]> CreateConsumer(string groupId, string topic)
        {
            InitTopic(topic).ConfigureAwait(false).GetAwaiter().GetResult();
            var dic = Options.CurrentValue.Properties;
            dic["group.id"] = groupId;
            return Consumers.GetOrAdd(groupId, _ =>
                new ConsumerBuilder<string, byte[]>(dic).Build()
            );
        }

        public void Dispose()
        {
            foreach (var value in Producers.Values)
            {
                try
                {
                    value.Dispose();
                }
                catch
                {
                    // ignored
                }
            }

            Producers.Clear();
            foreach (var value in Consumers.Values)
            {
                try
                {
                    value.Unsubscribe();
                    value.Close();
                    value.Dispose();
                }
                catch
                {
                    // ignored
                }
            }

            Consumers.Clear();
        }
    }
}