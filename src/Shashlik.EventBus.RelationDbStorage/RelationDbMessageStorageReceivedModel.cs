#nullable disable
using System.ComponentModel.DataAnnotations;
using FreeSqlIndex = FreeSql.DataAnnotations.IndexAttribute;
using Table = FreeSql.DataAnnotations.TableAttribute;

// ReSharper disable CheckNamespace

namespace Shashlik.EventBus
{
    /// <summary>
    /// 关系型数据库-已接收消息存储模型
    /// </summary>
    [Table]
    [FreeSqlIndex("ix_eventbus_received_msg_id_handler", "MsgId,EventHandlerName")]
    // 见 RelationDbMessageStoragePublishedModel 注释,这里对称设计
    [FreeSqlIndex("ix_eventbus_received_create_time", "CreateTimeTicks DESC,Status,IsDelay")]
    [FreeSqlIndex("ix_eventbus_received_expire_time", "Status,ExpireTimeTicks")]
    [FreeSqlIndex("ix_eventbus_received_delay", "IsDelay,DelayAtTicks")]
    public class RelationDbMessageStorageReceivedModel
    {
        /// <summary>
        /// 存储的消息id,由存储中间件自动生成
        /// </summary>
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// 消息id
        /// </summary>
        public string MsgId { get; set; }

        /// <summary>
        /// 环境
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// 事件名称
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// 事件处理名
        /// </summary>
        public string EventHandlerName { get; set; }

        /// <summary>
        /// 消息体
        /// </summary>
        public string EventBody { get; set; }

        /// <summary>
        /// 创建时间(UTC ticks)
        /// </summary>
        public long CreateTimeTicks { get; set; }

        /// <summary>
        /// 是否延迟消费
        /// </summary>
        public bool IsDelay { get; set; }

        /// <summary>
        /// 延迟消费时间(UTC ticks)
        /// </summary>
        public long? DelayAtTicks { get; set; }

        /// <summary>
        /// 过期时间,0永不过期(UTC ticks)
        /// </summary>
        public long? ExpireTimeTicks { get; set; }

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
        /// 锁定结束时间(UTC ticks)
        /// </summary>
        public long? LockEndTicks { get; set; }

        public override string ToString()
        {
            return $"{EventName}-{MsgId}";
        }
    }
}