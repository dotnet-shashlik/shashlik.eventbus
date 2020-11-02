// ReSharper disable UnusedAutoPropertyAccessor.Global

using System;
using RabbitMQ.Client;

namespace Shashlik.EventBus.RabbitMQ
{
    public class EventBusRabbitMQOptions
    {
        /// <summary>
        /// 正常通信交换机名称
        /// </summary>
        public string Exchange { get; set; } = "eventbus.ex";

        /// <summary>
        /// 死信交换机名称,用于延迟队列
        /// </summary>
        public string DeadExchange { get; set; } = "eventbus.ex.dead";

        /// <summary>
        /// 主机名称
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }

        public string VirtualHost { get; set; } = "/";

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; } = 5672;

        /// <summary>
        /// 自定义连接创建
        /// </summary>
        public Func<ConnectionFactory> ConnectionFactory { get; set; }
    }
}