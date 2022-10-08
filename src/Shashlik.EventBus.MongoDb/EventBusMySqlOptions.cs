using System;
using MongoDB.Driver;

namespace Shashlik.EventBus.MongoDb
{
    public class EventBusMongoDbOptions
    {
        /// <summary>
        /// 已发布消息表名
        /// </summary>
        public string PublishedCollectionName { get; set; } = "eventbus_published";

        /// <summary>
        /// 已接收的消息表名
        /// </summary>
        public string ReceivedCollectionName { get; set; } = "eventbus_received";

        /// <summary>
        /// 数据库名称
        /// </summary>
        public string DataBase { get; set; } = "eventbus";

        /// <summary>
        /// MongoDb数据库连接字符串
        /// </summary>
        public string? ConnectionString { get; set; } = "mongodb://localhost";
    }
}