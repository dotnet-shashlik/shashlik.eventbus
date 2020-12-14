using System.Collections.Concurrent;
using System.Threading;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.Kafka
{
    public class DefaultKafkaConnection : IKafkaConnection
    {
        public DefaultKafkaConnection(IOptionsMonitor<EventBusKafkaOptions> options)
        {
            Options = options;
        }

        private ConcurrentDictionary<int, IProducer<string, byte[]>> Producers { get; } =
            new ConcurrentDictionary<int, IProducer<string, byte[]>>();

        private ConcurrentDictionary<string, IConsumer<string, byte[]>> Consumers { get; } =
            new ConcurrentDictionary<string, IConsumer<string, byte[]>>();

        private IOptionsMonitor<EventBusKafkaOptions> Options { get; }

        public IProducer<string, byte[]> GetProducer()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            return Producers.GetOrAdd(id, r =>
                new ProducerBuilder<string, byte[]>(Options.CurrentValue.Properties.ConvertToDictionary()).Build()
            );
        }

        public IConsumer<string, byte[]> CreateConsumer(string groupId)
        {
            var dic = Options.CurrentValue.Properties.ConvertToDictionary();
            dic["group.id"] = groupId;
            return Consumers.GetOrAdd(groupId, r =>
                new ConsumerBuilder<string, byte[]>(dic).Build()
            );
        }

        public void Dispose()
        {
            try
            {
                foreach (var value in Producers.Values)
                {
                    value.Dispose();
                }

                Producers.Clear();
                foreach (var value in Consumers.Values)
                {
                    value.Unsubscribe();
                    value.Close();
                    value.Dispose();
                }

                Consumers.Clear();
            }
            catch
            {
                // ignored
            }
        }
    }
}