using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Pulsar.Client.Api;
using Pulsar.Client.Common;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.Pulsar
{
    public class DefaultPulsarConnection : IPulsarConnection, IAsyncDisposable
    {
        public DefaultPulsarConnection(
            IOptionsMonitor<EventBusPulsarOptions> options,
            IServiceProvider serviceProvider,
            IObjectPoolProvider poolProvider)
        {
            Options = options;
            _serviceProvider = serviceProvider;
            _connection = new Lazy<PulsarClient>(Get, true);
            _consumers = new ConcurrentDictionary<string, IConsumer<byte[]>>();
            _poolProvider = poolProvider;
        }

        private readonly IServiceProvider _serviceProvider;
        private readonly Lazy<PulsarClient> _connection;
        private readonly ConcurrentDictionary<string, IConsumer<byte[]>> _consumers;
        private readonly IObjectPoolProvider _poolProvider;
        private readonly ConcurrentDictionary<string, IObjectPool<IProducer<byte[]>>> _pools = new();
        private IOptionsMonitor<EventBusPulsarOptions> Options { get; }
        private PulsarClient Connection => _connection.Value;

        private PulsarClient Get()
        {
            if (Options.CurrentValue.PulsarClientFactory is not null)
                return Options.CurrentValue.PulsarClientFactory(_serviceProvider);
            return new PulsarClientBuilder().ServiceUrl(Options.CurrentValue.ServiceUrl)
                .BuildAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async ValueTask<IPoolLease<IProducer<byte[]>>> GetProducer(string topic)
        {
            return await _pools.GetOrAdd(topic, t => CreatePoolFor(t))
                .RentAsync().ConfigureAwait(false);
        }

        private IObjectPool<IProducer<byte[]>> CreatePoolFor(string topic)
        {
            return _poolProvider.Create<IProducer<byte[]>>(
                $"pulsar.producer.{topic}",
                new PulsarProducerPoolPolicy(Connection, topic),
                Math.Max(1, Environment.ProcessorCount));
        }

        public IConsumer<byte[]> GetConsumer(string topic, string group)
        {
            return _consumers.GetOrAdd($"{topic}-{group}", _ =>
                Connection.NewConsumer().Topic(topic).SubscriptionName(group).ConsumerName(group)
                    .SubscriptionType(SubscriptionType.Shared).SubscribeAsync().ConfigureAwait(false).GetAwaiter()
                    .GetResult());
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                foreach (var item in _consumers.Values)
                {
                    await item.DisposeAsync();
                }

                await Connection.CloseAsync();
                _consumers.Clear();
            }
            catch
            {
                // ignore
            }
        }
    }

    /// <summary>
    /// <see cref="IObjectPoolPolicy{IProducer}"/> 的 Pulsar 实现。
    /// Pulsar producer 是 thread-safe 的(内部 producer 状态机),可池化。
    /// </summary>
    internal class PulsarProducerPoolPolicy : IObjectPoolPolicy<IProducer<byte[]>>
    {
        private readonly PulsarClient _client;
        private readonly string _topic;

        public PulsarProducerPoolPolicy(PulsarClient client, string topic)
        {
            _client = client;
            _topic = topic;
        }

        public async ValueTask<IProducer<byte[]>> CreateAsync(CancellationToken cancellationToken = default)
        {
            return await _client.NewProducer().Topic(_topic).CreateAsync().ConfigureAwait(false);
        }

        public bool TryReuse(IProducer<byte[]> item)
        {
            // Pulsar producer 没有"closed"状态可查询,默认一直可复用
            return true;
        }
    }
}
