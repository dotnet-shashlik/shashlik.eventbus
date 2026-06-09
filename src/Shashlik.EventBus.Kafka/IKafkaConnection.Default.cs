using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.Kafka
{
    public class DefaultKafkaConnection : IKafkaConnection
    {
        public DefaultKafkaConnection(
            IOptionsMonitor<EventBusKafkaOptions> options,
            IObjectPoolProvider poolProvider)
        {
            Options = options;
            // producer 按 topic 各自一个池。每个 topic 一组 producer 上限。
            // 之所以不按 (topic) 一一映射到单例:Producer 在 librdkafka 里是 thread-safe 且
            // 内部带批处理,池化能压住 librdkafka 内部 queue 的同时让多个业务线程并发发布。
            _producers = poolProvider;
        }

        private IOptionsMonitor<EventBusKafkaOptions> Options { get; }
        private readonly IObjectPoolProvider _producers;
        private readonly ConcurrentDictionary<string, IObjectPool<IProducer<string, byte[]>>> _pools = new();
        private readonly ConcurrentDictionary<string, IConsumer<string, byte[]>> _consumers = new();
        private readonly ConcurrentBag<string> _existsTopics = new();

        private async Task EnsureTopicAsync(string topic)
        {
            if (_existsTopics.Any(t => t == topic))
                return;
            try
            {
                var config = new AdminClientConfig(Options.CurrentValue.Properties);
                using var adminClient = new AdminClientBuilder(config).Build();
                await adminClient.CreateTopicsAsync(new[] { NewTopicSpecification(topic) })
                    .ConfigureAwait(false);
                _existsTopics.Add(topic);
            }
            catch (CreateTopicsException ex)
            {
                if (!ex.Message.Contains("exists", StringComparison.OrdinalIgnoreCase))
                    throw;
                _existsTopics.Add(topic);
            }
        }

        private TopicSpecification NewTopicSpecification(string name)
        {
            var spec = new TopicSpecification
            {
                NumPartitions = Options.CurrentValue.TopicNumPartitions,
                ReplicationFactor = Options.CurrentValue.TopicReplicationFactor
            };
            Options.CurrentValue.ConfigTopic?.Invoke(spec);
            spec.Name = name;
            return spec;
        }

        public async ValueTask<IPoolLease<IProducer<string, byte[]>>> GetProducer(string topic)
        {
            return await _pools.GetOrAdd(topic, t => CreatePoolFor(t)).RentAsync().ConfigureAwait(false);
        }

        private IObjectPool<IProducer<string, byte[]>> CreatePoolFor(string topic)
        {
            return _producers.Create<IProducer<string, byte[]>>(
                $"kafka.producer.{topic}",
                new KafkaProducerPoolPolicy(Options),
                Math.Max(1, Environment.ProcessorCount));
        }

        public IConsumer<string, byte[]> CreateConsumer(string groupId, string topic)
        {
            EnsureTopicAsync(topic).ConfigureAwait(false).GetAwaiter().GetResult();
            var dic = new Dictionary<string, string>(Options.CurrentValue.Properties, StringComparer.Ordinal);
            dic["group.id"] = groupId;
            // Confluent.Kafka 2.x defaults enable.auto.offset.store=true, which means
            // every Consume() call auto-stores the offset. Shashlik's "NACK on failure"
            // semantics (StoreOffset only when OnReceiveAsync returns Success) become
            // a no-op. Force false unless the user explicitly set the key.
            if (!dic.ContainsKey("enable.auto.offset.store"))
                dic["enable.auto.offset.store"] = "false";
            return _consumers.GetOrAdd(groupId, _ =>
                new ConsumerBuilder<string, byte[]>(dic).Build()
            );
        }

        public void Dispose()
        {
            // 池里的 producer 在自身 dispose 时会随池释放
            foreach (var c in _consumers.Values)
            {
                try { c.Unsubscribe(); c.Close(); c.Dispose(); }
                catch { }
            }

            _consumers.Clear();
        }
    }

    /// <summary>
    /// <see cref="IObjectPoolPolicy{IProducer}"/> 的 Kafka 实现。
    /// Producer 线程安全,可直接池化。复用前不强制检查(Producer 没有"关闭"概念),
    /// 由池在 Return 时根据 policy 决定。
    /// </summary>
    internal class KafkaProducerPoolPolicy : IObjectPoolPolicy<IProducer<string, byte[]>>
    {
        private readonly IOptionsMonitor<EventBusKafkaOptions> _options;

        public KafkaProducerPoolPolicy(IOptionsMonitor<EventBusKafkaOptions> options)
        {
            _options = options;
        }

        public ValueTask<IProducer<string, byte[]>> CreateAsync(CancellationToken cancellationToken = default)
        {
            var builder = new ProducerBuilder<string, byte[]>(_options.CurrentValue.Properties);
            return new ValueTask<IProducer<string, byte[]>>(builder.Build());
        }

        public bool TryReuse(IProducer<string, byte[]> item)
        {
            // librdkafka 的 producer 始终可复用(线程安全 + 内部 batch queue)
            return true;
        }
    }
}
