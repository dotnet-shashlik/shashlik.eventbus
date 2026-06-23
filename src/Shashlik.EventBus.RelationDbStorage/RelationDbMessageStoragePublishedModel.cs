#nullable disable
using System.ComponentModel.DataAnnotations;
using FreeSqlIndex = FreeSql.DataAnnotations.IndexAttribute;
using Table = FreeSql.DataAnnotations.TableAttribute;

// ReSharper disable CheckNamespace

namespace Shashlik.EventBus
{
    /// <summary>
    /// 关系型数据库-已发布消息存储模型
    /// </summary>
    [Table]
    [FreeSqlIndex("ix_eventbus_published_msg_id", nameof(MsgId), isUnique: true)]
    // (CreateTimeTicks DESC, Status, IsDelay): retry 查询主索引
    //   - 第 1 列对应 ORDER BY create_time_ticks DESC, 避免 filesort
    //   - 第 2 列 status 是 IN 范围条件, 作为 backward scan 时的早停 filter
    //   - 第 3 列 is_delay 让两个分支共用此索引, 同时让 normal 分支 (is_delay=0) 在
    //     扫描时即可过滤掉延迟消息行 (替代旧的 event_name, 后者在 retry 路径完全不参与)
    [FreeSqlIndex("ix_eventbus_published_create_time", "CreateTimeTicks DESC,Status,IsDelay")]
    // (Status, ExpireTimeTicks): DeleteExpired 用; 删除原索引中间无关的 RetryCount 列
    // 让 (status='SUCCEEDED', expire_time_ticks < now) 形成连续 range, 避免 skip scan
    [FreeSqlIndex("ix_eventbus_published_expire_time", "Status,ExpireTimeTicks")]
    // (IsDelay, DelayAtTicks): 专给 retry 查询的 delay 分支 (is_delay=1 AND delay_at_ticks <= now)
    // 否则该分支会回退到 ix_create_time 全索引扫描, 在没有延迟消息时也要扫整表
    [FreeSqlIndex("ix_eventbus_published_delay", "IsDelay,DelayAtTicks")]
    public class RelationDbMessageStoragePublishedModel
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
        /// 환경变量
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
        /// 创建时间(UTC ticks;FreeSql.Provider.Sqlite 对 DateTime 做了 ToLocalTime
        /// 处理,DateTimeOffset 又会被 CodeFirst 静默丢掉,所以用 long ticks 存绝对时间)
        /// </summary>
        public long CreateTimeTicks { get; set; }

        /// <summary>
        /// 是否延迟消息
        /// </summary>
        public bool IsDelay { get; set; }

        /// <summary>
        /// 延迟消费时间(UTC ticks)
        /// </summary>
        public long? DelayAtTicks { get; set; }

        /// <summary>
        /// 过期时间(UTC ticks)
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
    }
}