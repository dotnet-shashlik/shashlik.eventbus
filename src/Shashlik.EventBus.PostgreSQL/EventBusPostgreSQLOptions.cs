#nullable enable
using System;

namespace Shashlik.EventBus.PostgreSQL
{
    public class EventBusPostgreSQLOptions
    {
        /// <summary>
        /// 模式名
        /// </summary>
        public string Schema { get; set; } = "eventbus";

        /// <summary>
        /// 已发布消息表名
        /// </summary>
        public string PublishTableName { get; set; } = "publish";

        /// <summary>
        /// 已接收的消息表名
        /// </summary>
        public string ReceiveTableName { get; set; } = "receive";

        /// <summary>
        /// ef数据库上下文类型, 和<see cref="ConnectionString"/>配其中一个，优先使用<see cref="DbContextType"/>
        /// </summary>
        public Type? DbContextType { get; set; }

        /// <summary>
        /// mysql数据库连接字符串，和<see cref="DbContextType"/>配其中一个，优先使用<see cref="DbContextType"/>
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// 已发布消息表全名
        /// </summary>
        public string FullPublishTableName => $"\"{Schema}\".\"{PublishTableName}\"";

        /// <summary>
        /// 已接收的消息表全名
        /// </summary>
        public string FullReceiveTableName => $"\"{Schema}\".\"{ReceiveTableName}\"";
    }
}
