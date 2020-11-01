using System;
using RabbitMQ.Client;

namespace Shashlik.EventBus.RabbitMQ
{
    public interface IRabbitMQConnection
    {
        IModel GetChannel();
    }
}