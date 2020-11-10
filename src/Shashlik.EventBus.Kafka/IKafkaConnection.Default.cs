using System;
using Confluent.Kafka;

namespace Shashlik.EventBus.Kafka
{
    public class DefaultKafkaConnection : IKafkaConnection, IDisposable
    {
        public IProducer<string, byte[]> GetProducer()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}