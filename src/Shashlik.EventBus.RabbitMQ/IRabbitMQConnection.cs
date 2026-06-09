using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.RabbitMQ
{
    public interface IRabbitMQConnection
    {
        /// <summary>
        /// 借一个 RabbitMQ channel。返回 <see cref="IPoolLease{IChannel}"/>,
        /// 应当使用 <c>await using var lease = await conn.GetChannelAsync()</c>,
        /// 离开作用域时 channel 自动归还到池(无效 channel 会被丢弃)。
        /// </summary>
        ValueTask<IPoolLease<IChannel>> GetChannelAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建一个异步消息消费者,长期持有(不参与池租借)。
        /// </summary>
        AsyncEventingBasicConsumer CreateConsumer(string eventHandlerName, IChannel channel);
    }
}
