using Confluent.Kafka;

namespace Shashlik.EventBus.Kafka
{
    public interface IKafkaConnection
    {
        IProducer<string, byte[]> GetProducer();
    }
}