using System;
using Pulsar.Client.Api;

namespace Shashlik.EventBus.Pulsar
{
    public class EventBusPulsarOptions
    {
        /// <summary>
        /// 连接
        /// </summary>
        public string? ServiceUrl { get; set; } = "pulsar://localhost";

        /// <summary>
        /// 消费池大小,默认4
        /// </summary>
        public int ConsumerPoolSize { get; set; } = 4;

        public Func<IServiceProvider, PulsarClient>? PulsarClientFactory { get; set; }
    }
}