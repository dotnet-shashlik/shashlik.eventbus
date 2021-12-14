// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Shashlik.EventBus
{
    public class EventBusOptions
    {
        /// <summary>
        /// 环境变量,用于区分事件/消费者,默认: Production
        /// </summary>
        public string Environment { get; set; } = "Production";

        /// <summary>
        /// 确认事务是否已提交的时间,单位秒,默认60s,必须小于<see cref="StartRetryAfter"/>
        /// </summary>
        public int TransactionCommitTimeout { get; set; } = 60;

        /// <summary>
        /// 重试器在消息处理失败多久之后开始执行,单位秒,默认300s
        /// </summary>
        public int StartRetryAfter { get; set; } = 60 * 5;

        /// <summary>
        /// 重试器单次执行数量,默认100
        /// </summary>
        public int RetryLimitCount { get; set; } = 100;

        /// <summary>
        /// 最大失败重试次数,默认60次,最小值5
        /// </summary>
        public int RetryFailedMax { get; set; } = 60;

        /// <summary>
        /// 失败重试间隔,单位秒,默认120s
        /// </summary>
        public int RetryInterval { get; set; } = 60 * 2;

        /// <summary>
        /// 执行重试操作时,锁定时长,秒,默认110s,需要小于<see cref="RetryInterval"/>
        /// </summary>
        public int LockTime { get; set; } = 60 * 2 - 10;

        /// <summary>
        /// 成功的消息多久后删除,单位小时,默认3天
        /// </summary>
        public int SucceedExpireHour { get; set; } = 3 * 24;
    }
}