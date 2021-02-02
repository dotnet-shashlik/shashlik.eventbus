using System;
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
                new ProducerBuilder<string, byte[]>(Options.CurrentValue.Properties).Build()
            );
        }

        public IConsumer<string, byte[]> CreateConsumer(string groupId)
        {
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
                catch (Exception e)
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
                catch (Exception e)
                {
                    // ignored
                }
            }

            Consumers.Clear();
        }
    }
}