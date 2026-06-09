using System.Threading.Tasks;
using Pulsar.Client.Api;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.Pulsar
{
    public interface IPulsarConnection
    {
        /// <summary>
        /// 借一个 topic 对应的 Pulsar <see cref="IProducer{T}"/>。应当
        /// <c>await using var lease = await conn.GetProducer(topic)</c>,
        /// 离开作用域时归还到池。
        /// </summary>
        ValueTask<IPoolLease<IProducer<byte[]>>> GetProducer(string topic);

        /// <summary>
        /// 创建一个长期持有的消费者(不走池租借)。
        /// </summary>
        IConsumer<byte[]> GetConsumer(string topic, string eventHandlerName);
    }
}
