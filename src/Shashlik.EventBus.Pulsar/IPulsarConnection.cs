using Pulsar.Client.Api;

namespace Shashlik.EventBus.Pulsar
{
    public interface IPulsarConnection
    {
        IProducer<byte[]> GetProducer(string topic);

        IConsumer<byte[]> GetConsumer(string topic, string eventHandlerName);
    }
}