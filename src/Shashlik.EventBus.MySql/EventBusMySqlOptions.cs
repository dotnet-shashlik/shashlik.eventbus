#nullable enable
using System;

namespace Shashlik.EventBus.MySql
{
    public class EventBusMySqlOptions
    {
        /// <summary>
        /// 已发布消息表名
        /// </summary>
        public string PublishTableName { get; set; } = "eventbus_publish";

        /// <summary>
        /// 已接收的消息表名
        /// </summary>
        public string ReceiveTableName { get; set; } = "eventbus_receive";

        /// <summary>
        /// ef数据库上下文类型, 和<see cref="ConnectionString"/>配其中一个，优先使用<see cref="DbContextType"/>
        /// </summary>
        public Type? DbContextType { get; set; }

        /// <summary>
        /// mysql数据库连接字符串，和<see cref="DbContextType"/>配其中一个，优先使用<see cref="DbContextType"/>
        /// </summary>
        public string? ConnectionString { get; set; }
    }
}