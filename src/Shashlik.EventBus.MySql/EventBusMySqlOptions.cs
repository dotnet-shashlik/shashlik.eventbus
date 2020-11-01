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
        /// ef数据库上下文类型
        /// </summary>
        public Type DbContextType { get; set; }

        /// <summary>
        /// mysql数据库连接字符串
        /// </summary>
        public string ConnectionString { get; set; }
    }
}