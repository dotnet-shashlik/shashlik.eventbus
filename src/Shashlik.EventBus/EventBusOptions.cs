// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace Shashlik.EventBus
{
    public class EventBusOptions
    {
        /// <summary>
        /// 环境变量,用于区分事件/消费者,默认空
        /// </summary>
        public string Environment { get; set; } = "Production";

        /// <summary>
        /// 确认事务是否已提交的时间,单位秒,默认3分钟,必须小于<see cref="RetryAfterSeconds"/>
        /// </summary>
        public int ConfirmTransactionSeconds { get; set; } = 60 * 3;

        /// <summary>
        /// 重试器在消息处理失败后多久之后执行,单位秒,默认5分钟
        /// </summary>
        public int RetryAfterSeconds { get; set; } = 60 * 5;

        /// <summary>
        /// 重试机制并行数量,默认5
        /// </summary>
        public int RetryMaxDegreeOfParallelism { get; set; } = 5;

        /// <summary>
        /// 重试器单次执行数量,默认100
        /// </summary>
        public int RetryLimitCount { get; set; } = 100;

        /// <summary>
        /// 失败重试次数,默认60次
        /// </summary>
        public int RetryFailedMax { get; set; } = 60;

        /// <summary>
        /// 失败重试间隔,单位分钟,默认2分钟
        /// </summary>
        public int RetryIntervalSeconds { get; set; } = 60 * 2;

        /// <summary>
        /// 成功的消息多久后删除,单位小时,默认3天
        /// </summary>
        public int SucceedExpireHour { get; set; } = 3 * 24;
    }
}