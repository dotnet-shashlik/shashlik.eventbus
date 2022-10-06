using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Pulsar.Client.Api;
using Pulsar.Client.Common;

namespace Shashlik.EventBus.Pulsar
{
    public class DefaultPulsarConnection : IPulsarConnection, IAsyncDisposable
    {
        public DefaultPulsarConnection(IOptionsMonitor<EventBusPulsarOptions> options, IServiceProvider serviceProvider)
        {
            Options = options;
            _serviceProvider = serviceProvider;
            _connection = new Lazy<PulsarClient>(Get, true);
            _producers = new ConcurrentDictionary<string, IProducer<byte[]>>();
            _consumers = new ConcurrentDictionary<string, IConsumer<byte[]>>();
        }

        private readonly IServiceProvider _serviceProvider;
        private readonly Lazy<PulsarClient> _connection;
        private readonly ConcurrentDictionary<string, IProducer<byte[]>> _producers;
        private readonly ConcurrentDictionary<string, IConsumer<byte[]>> _consumers;
        private IOptionsMonitor<EventBusPulsarOptions> Options { get; }
        private PulsarClient Connection => _connection.Value;

        private PulsarClient Get()
        {
            if (Options.CurrentValue.PulsarClientFactory is not null)
                return Options.CurrentValue.PulsarClientFactory(_serviceProvider);
            return new PulsarClientBuilder().ServiceUrl(Options.CurrentValue.ServiceUrl)
                .BuildAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public IProducer<byte[]> GetProducer(string topic)
        {
            return _producers.GetOrAdd(topic,
                _ => Connection.NewProducer().Topic(topic).CreateAsync()
                    .ConfigureAwait(false).GetAwaiter().GetResult());
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
                foreach (var item in _producers.Values)
                {
                    await item.DisposeAsync();
                }

                foreach (var item in _consumers.Values)
                {
                    await item.DisposeAsync();
                }

                await Connection.CloseAsync();
                _producers.Clear();
                _consumers.Clear();
            }
            catch
            {
                // ignore
            }
        }
    }
}