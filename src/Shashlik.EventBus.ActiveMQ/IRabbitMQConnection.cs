using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Shashlik.EventBus.RabbitMQ
{
    public interface IRabbitMQConnection
    {
        IModel GetChannel();

        EventingBasicConsumer CreateConsumer(string eventHandlerName);
    }
}