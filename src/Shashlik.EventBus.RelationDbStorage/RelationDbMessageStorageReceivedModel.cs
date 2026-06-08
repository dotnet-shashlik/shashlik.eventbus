#nullable disable
using System.ComponentModel.DataAnnotations.Schema;
using FreeSql.DataAnnotations;
using Column = FreeSql.DataAnnotations.ColumnAttribute;
using FreeSqlIndex = FreeSql.DataAnnotations.IndexAttribute;
using Table = FreeSql.DataAnnotations.TableAttribute;

// ReSharper disable CheckNamespace

namespace Shashlik.EventBus
{
    /// <summary>
    /// 关系型数据库-已接收消息存储模型
    /// </summary>
    [Table(Name = "eventbus_received")]
    [FreeSqlIndex("ix_eventbus_received_msgId_handler", nameof(MsgId) + "," + nameof(EventHandlerName), IsUnique = true)]
    [FreeSqlIndex("ix_eventbus_received_status_isDelay_delayAt", nameof(Status) + "," + nameof(IsDelay) + "," + nameof(DelayAt))]
    [FreeSqlIndex("ix_eventbus_received_status_expireTime", nameof(Status) + "," + nameof(ExpireTime))]
    [FreeSqlIndex("ix_eventbus_received_status_lockEnd", nameof(Status) + "," + nameof(LockEnd))]
    public class RelationDbMessageStorageReceivedModel
    {
        /// <summary>
        /// 存储的消息id,由存储中间件自动生成
        /// </summary>
        [Column(Name = "id", IsIdentity = true)]
        public long Id { get; set; }

        /// <summary>
        /// 消息id
        /// </summary>
        [Column(Name = "msgId")]
        public string MsgId { get; set; }

        /// <summary>
        /// 环境
        /// </summary>
        [Column(Name = "environment")]
        public string Environment { get; set; }

        /// <summary>
        /// 事件名称
        /// </summary>
        [Column(Name = "eventName")]
        public string EventName { get; set; }

        /// <summary>
        /// 事件处理名
        /// </summary>
        [Column(Name = "eventHandlerName")]
        public string EventHandlerName { get; set; }

        /// <summary>
        /// 消息体
        /// </summary>
        [Column(Name = "eventBody")]
        public string EventBody { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column(Name = "createTime")]
        public long CreateTime { get; set; }

        /// <summary>
        /// 是否延迟消费
        /// </summary>
        [Column(Name = "isDelay")]
        public bool IsDelay { get; set; }

        /// <summary>
        /// 延迟消费时间
        /// </summary>
        [Column(Name = "delayAt")]
        public long? DelayAt { get; set; }

        /// <summary>
        /// 过期时间,0永不过期
        /// </summary>
        [Column(Name = "expireTime")]
        public long? ExpireTime { get; set; }

        /// <summary>
        /// 事件附加数据
        /// </summary>
        [Column(Name = "eventItems")]
        public string EventItems { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        [Column(Name = "status")]
        public string Status { get; set; }

        /// <summary>
        /// 已重试次数
        /// </summary>
        [Column(Name = "retryCount")]
        public int RetryCount { get; set; }

        /// <summary>
        /// 是否已锁定
        /// </summary>
        [Column(Name = "isLocking")]
        public bool IsLocking { get; set; }

        /// <summary>
        /// 锁定结束时间
        /// </summary>
        [Column(Name = "lockEnd")]
        public long? LockEnd { get; set; }

        public override string ToString()
        {
            return $"{EventName}-{MsgId}";
        }
    }
}
