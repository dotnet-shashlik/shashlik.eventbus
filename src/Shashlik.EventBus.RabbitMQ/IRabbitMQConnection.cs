using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Shashlik.EventBus.RabbitMQ
{
    public interface IRabbitMQConnection
    {
        /// <summary>
        /// 获取(并按需创建)一个异步 channel
        /// </summary>
        ValueTask<IChannel> GetChannelAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建一个异步消息消费者
        /// </summary>
        /// <param name="eventHandlerName">事件处理名称(同时作为 queue 名和 consumer tag)</param>
        /// <param name="channel">要绑定的 channel</param>
        AsyncEventingBasicConsumer CreateConsumer(string eventHandlerName, IChannel channel);
    }
}
