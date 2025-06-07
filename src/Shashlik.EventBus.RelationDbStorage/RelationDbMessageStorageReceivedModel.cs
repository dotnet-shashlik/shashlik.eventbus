#nullable disable
using System;
using System.ComponentModel.DataAnnotations.Schema;

// ReSharper disable CheckNamespace

namespace Shashlik.EventBus
{
    /// <summary>
    /// 消息存储模型
    /// </summary>
    public class RelationDbMessageStorageReceivedModel
    {
        /// <summary>
        /// 存储的消息id,由存储中间件自动生成
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public long Id { get; set; }

        /// <summary>
        /// 消息id
        /// </summary>
        [Column("msgId")]
        public string MsgId { get; set; }

        /// <summary>
        /// 环境
        /// </summary>
        [Column("environment")]
        public string Environment { get; set; }

        /// <summary>
        /// 事件名称
        /// </summary>
        [Column("eventName")]
        public string EventName { get; set; }

        /// <summary>
        /// 事件处理名
        /// </summary>
        [Column("eventHandlerName")]
        public string EventHandlerName { get; set; }

        /// <summary>
        /// 消息体
        /// </summary>
        [Column("eventBody")]
        public string EventBody { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("createTime")]
        public long CreateTime { get; set; }

        /// <summary>
        /// 是否延迟消费
        /// </summary>
        [Column("isDelay")]
        public bool IsDelay { get; set; }

        /// <summary>
        /// 延迟消费时间
        /// </summary>
        [Column("delayAt")]
        public long? DelayAt { get; set; }

        /// <summary>
        /// 过期时间,0永不过期
        /// </summary>
        [Column("expireTime")]
        public long? ExpireTime { get; set; }

        /// <summary>
        /// 事件附加数据
        /// </summary>
        [Column("eventItems")]
        public string EventItems { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        [Column("status")]
        public string Status { get; set; }

        /// <summary>
        /// 已重试次数
        /// </summary>
        [Column("retryCount")]
        public int RetryCount { get; set; }

        /// <summary>
        /// 是否已锁定
        /// </summary>
        [Column("isLocking")]
        public bool IsLocking { get; set; }

        /// <summary>
        /// 锁定结束时间
        /// </summary>
        [Column("lockEnd")]
        public long? LockEnd { get; set; }

        public override string ToString()
        {
            return $"{EventName}-{MsgId}";
        }
    }
}