using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
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

        private ConcurrentDictionary<string, IProducer<string, byte[]>> Producers { get; } =
            new ConcurrentDictionary<string, IProducer<string, byte[]>>();

        private ConcurrentDictionary<string, IConsumer<string, byte[]>> Consumers { get; } =
            new ConcurrentDictionary<string, IConsumer<string, byte[]>>();

        private IOptionsMonitor<EventBusKafkaOptions> Options { get; }
        private ConcurrentBag<string> ExistsTopics { get; } = new ConcurrentBag<string>();

        private async Task InitTopic(string topic)
        {
            if (ExistsTopics.Contains(topic))
                return;
            try
            {
                var dic = Options.CurrentValue.Properties;
                var config = new AdminClientConfig(dic);
                using var adminClient = new AdminClientBuilder(config).Build();
                await adminClient.CreateTopicsAsync(new[] {new TopicSpecification {Name = topic}}).ConfigureAwait(false);
                ExistsTopics.Add(topic);
            }
            catch (CreateTopicsException ex)
            {
                if (!ex.Message.Contains("exists", StringComparison.OrdinalIgnoreCase))
                    throw;
                ExistsTopics.Add(topic);
            }
        }

        public IProducer<string, byte[]> GetProducer(string topic)
        {
            InitTopic(topic).ConfigureAwait(false).GetAwaiter().GetResult();
            return Producers.GetOrAdd(topic, r =>
                new ProducerBuilder<string, byte[]>(Options.CurrentValue.Properties).Build()
            );
        }

        public IConsumer<string, byte[]> CreateConsumer(string groupId, string topic)
        {
            InitTopic(topic).ConfigureAwait(false).GetAwaiter().GetResult();
            var dic = Options.CurrentValue.Properties;
            dic["group.id"] = groupId;
            return Consumers.GetOrAdd(groupId, r =>
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