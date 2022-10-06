using System;
using RabbitMQ.Client;

namespace Shashlik.EventBus.RabbitMQ
{
    public class EventBusRabbitMQOptions
    {
        /// <summary>
        /// 交换机名称
        /// </summary>
        public string Exchange { get; set; } = "shashlik.eventbus";

        /// <summary>
        /// 主机名称
        /// </summary>
        public string Host { get; set; } = "localhost";

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; } = "guest";

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; } = "guest";

        /// <summary>
        /// virtual host, default: /
        /// </summary>
        public string VirtualHost { get; set; } = "/";

        /// <summary>
        /// 端口号, default: 5672
        /// </summary>
        public int Port { get; set; } = 5672;

        /// <summary>
        /// 发送消息确认超时时间,单位秒,default:5s
        /// </summary>
        public int ConfirmTimeout { get; set; } = 5;

        /// <summary>
        /// 自定义连接创建,优先使用此属性
        /// </summary>
        public Func<ConnectionFactory>? ConnectionFactory { get; set; }
    }
}