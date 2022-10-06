#nullable disable
using System;

// ReSharper disable CheckNamespace

namespace Shashlik.EventBus
{
    /// <summary>
    /// 消息存储模型
    /// </summary>
    public class MessageStorageModel
    {
        /// <summary>
        /// 存储的消息id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 消息d
        /// </summary>
        public string MsgId { get; set; }

        /// <summary>
        /// 环境变量
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTimeOffset CreateTime { get; set; }

        /// <summary>
        /// 延迟消费时间
        /// </summary>
        public DateTimeOffset? DelayAt { get; set; }

        /// <summary>
        /// 过期时间,0永不过期
        /// </summary>
        public DateTimeOffset? ExpireTime { get; set; }

        /// <summary>
        /// 事件处理名称
        /// </summary>
        public string EventHandlerName { get; set; }

        /// <summary>
        /// 事件名称
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// 事件内容
        /// </summary>
        public string EventBody { get; set; }

        /// <summary>
        /// 事件附加数据
        /// </summary>
        public string EventItems { get; set; }

        /// <summary>
        /// 已重试次数
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 是否已锁定
        /// </summary>
        public bool IsLocking { get; set; }

        /// <summary>
        /// 锁定结束时间
        /// </summary>
        public DateTimeOffset? LockEnd { get; set; }

        public override string ToString()
        {
            return $"{EventName}-{MsgId}";
        }
    }
}