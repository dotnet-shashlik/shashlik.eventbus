using System;
using Pulsar.Client.Api;

namespace Shashlik.EventBus.Pulsar
{
    public class EventBusPulsarOptions
    {
        /// <summary>
        /// 连接
        /// </summary>
        public string? ServiceUrl { get; set; }

        public Func<IServiceProvider, PulsarClient>? PulsarClientFactory { get; set; }
    }
}