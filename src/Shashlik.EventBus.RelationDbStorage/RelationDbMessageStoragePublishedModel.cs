#nullable disable
using System;
using Column = FreeSql.DataAnnotations.ColumnAttribute;
using FreeSqlIndex = FreeSql.DataAnnotations.IndexAttribute;
using Table = FreeSql.DataAnnotations.TableAttribute;

// ReSharper disable CheckNamespace

namespace Shashlik.EventBus
{
    /// <summary>
    /// 关系型数据库-已发布消息存储模型
    /// </summary>
    [Table]
    [FreeSqlIndex("ix_eventbus_published_msg_id", nameof(MsgId), IsUnique = true)]
    [FreeSqlIndex("ix_eventbus_published_create_time", "CreateTime DESC,Status,EventName")]
    [FreeSqlIndex("ix_eventbus_published_expire_time", "Status,RetryCount,ExpireTime")]
    [FreeSqlIndex("ix_eventbus_published_retry", "IsDelay,Status,IsLocking,DelayAt,RetryCount,CreateTime DESC")]
    public class RelationDbMessageStoragePublishedModel
    {
        /// <summary>
        /// 存储的消息id,由存储中间件自动生成
        /// </summary>
        [Column(IsIdentity = true)]
        public long Id { get; set; }

        /// <summary>
        /// 消息id
        /// </summary>
        public string MsgId { get; set; }

        /// <summary>
        /// 环境变量
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// 事件名称
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// 事件内容
        /// </summary>
        public string EventBody { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTimeOffset CreateTime { get; set; }

        /// <summary>
        /// 是否延迟消息
        /// </summary>
        public bool IsDelay { get; set; }

        /// <summary>
        /// 延迟消费时间
        /// </summary>
        public DateTimeOffset? DelayAt { get; set; }

        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTimeOffset? ExpireTime { get; set; }

        /// <summary>
        /// 事件附加数据
        /// </summary>
        public string EventItems { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 已重试次数
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// 是否已锁定
        /// </summary>
        public bool IsLocking { get; set; }

        /// <summary>
        /// 锁定结束时间
        /// </summary>
        public DateTimeOffset? LockEnd { get; set; }
    }
}