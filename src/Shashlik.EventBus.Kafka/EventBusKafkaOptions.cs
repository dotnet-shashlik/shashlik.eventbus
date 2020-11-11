using Confluent.Kafka;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.Kafka
{
    public class EventBusKafkaOptions
    {
        /// <summary>
        /// 基础配置
        /// </summary>
        public ClientConfig Base { get; set; } = new ClientConfig();

        /// <summary>
        /// 生产者配置
        /// </summary>
        public ProducerConfig Producer { get; set; } = new ProducerConfig();

        /// <summary>
        /// 消费者配置
        /// </summary>
        public ConsumerConfig Consumer { get; set; } = new ConsumerConfig();
    }
}