using System;

// ReSharper disable CheckNamespace

namespace Shashlik.EventBus
{
    /// <summary>
    /// 消息存储模型(在存储层和上层之间的统一 DTO)。
    /// 设计原则:
    ///   - 创建后一般不变更的字段使用 <c>init</c> 访问器(防误改)。
    ///   - 真正会变的字段(<c>Id</c> 自增回填, <c>Status</c>/<c>RetryCount</c>/<c>ExpireTime</c>/<c>EventHandlerName</c> 在重试/状态流转时更新)保留 <c>set</c>。
    /// </summary>
    public class MessageStorageModel
    {
        /// <summary>
        /// 存储的消息 id,由存储中间件自动生成,Insert 后回填
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 业务消息 id(全局唯一)
        /// </summary>
        public string MsgId { get; init; } = string.Empty;

        /// <summary>
        /// 环境变量
        /// </summary>
        public string? Environment { get; init; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTimeOffset CreateTime { get; init; }

        /// <summary>
        /// 延迟消费时间
        /// </summary>
        public DateTimeOffset? DelayAt { get; init; }

        /// <summary>
        /// 过期时间,<c>null</c> 永不过期
        /// </summary>
        public DateTimeOffset? ExpireTime { get; set; }

        /// <summary>
        /// 事件处理名称(已发布消息为 <c>null</c>)
        /// </summary>
        public string? EventHandlerName { get; set; }

        /// <summary>
        /// 事件名称
        /// </summary>
        public string EventName { get; init; } = default!;

        /// <summary>
        /// 事件内容(序列化后的字符串)
        /// </summary>
        public string EventBody { get; init; } = default!;

        /// <summary>
        /// 事件附加数据(序列化后的字符串)
        /// </summary>
        public string? EventItems { get; init; }

        /// <summary>
        /// 已重试次数(实际为"已执行次数",从 0 起,首次成功后变为 1)
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// 状态(参见 <see cref="MessageStatus"/>)
        /// </summary>
        public string Status { get; set; } = string.Empty;

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
