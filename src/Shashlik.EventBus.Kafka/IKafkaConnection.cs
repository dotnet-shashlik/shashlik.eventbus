using System;
using System.Threading.Tasks;
using Confluent.Kafka;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.Kafka
{
    public interface IKafkaConnection : IDisposable
    {
        /// <summary>
        /// 借一个 topic 对应的 <see cref="IProducer{TKey,TValue}"/>。应当
        /// <c>await using var lease = await conn.GetProducer(topic)</c>,
        /// 离开作用域时归还到池(producer 在 Kafka 协议上是线程安全的,可以池化)。
        /// </summary>
        ValueTask<IPoolLease<IProducer<string, byte[]>>> GetProducer(string topic);

        /// <summary>
        /// 创建一个消息消费者(长期持有,不走池租借)。
        /// </summary>
        IConsumer<string, byte[]> CreateConsumer(string groupId, string topic);
    }
}
