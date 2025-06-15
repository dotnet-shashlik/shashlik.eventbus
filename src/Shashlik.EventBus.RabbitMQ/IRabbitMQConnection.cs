using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Shashlik.EventBus.RabbitMQ
{
    public interface IRabbitMQConnection
    {
        IChannel GetChannel();

        AsyncEventingBasicConsumer CreateConsumer(string eventHandlerName);
    }
}