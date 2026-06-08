// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        /// 重试器并行执行数量,默认5
        /// </summary>
        public int RetryMaxDegreeOfParallelism { get; set; } = 5;

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

        /// <summary>
        /// Service注册生命周期类型
        /// </summary>
        public ServiceLifetime HandlerServiceLifetime { get; set; } = ServiceLifetime.Transient;
    }

    /// <summary>
    /// <see cref="EventBusOptions"/> 的校验器,实现 <see cref="IValidateOptions{T}"/>,
    /// 在应用启动时 (ValidateOnStart 开启) 或首次解析 Options 时自动触发。
    /// 检查各项参数的范围和相互关系,避免出现重试窗口 / 锁窗口 / 事务等待窗口错位。
    /// </summary>
    public class EventBusOptionsValidation : IValidateOptions<EventBusOptions>
    {
        public ValidateOptionsResult Validate(string? name, EventBusOptions options)
        {
            var errors = new List<string>();

            if (options.TransactionCommitTimeout <= 0)
                errors.Add($"{nameof(options.TransactionCommitTimeout)} must be > 0, got {options.TransactionCommitTimeout}");
            if (options.StartRetryAfter <= 0)
                errors.Add($"{nameof(options.StartRetryAfter)} must be > 0, got {options.StartRetryAfter}");
            if (options.TransactionCommitTimeout >= options.StartRetryAfter)
                errors.Add(
                    $"{nameof(options.TransactionCommitTimeout)} ({options.TransactionCommitTimeout}) must be < {nameof(options.StartRetryAfter)} ({options.StartRetryAfter}); otherwise the publish path will give up before the retry path even starts.");
            if (options.RetryInterval <= 0)
                errors.Add($"{nameof(options.RetryInterval)} must be > 0, got {options.RetryInterval}");
            if (options.LockTime <= 0)
                errors.Add($"{nameof(options.LockTime)} must be > 0, got {options.LockTime}");
            if (options.LockTime >= options.RetryInterval)
                errors.Add(
                    $"{nameof(options.LockTime)} ({options.LockTime}) must be < {nameof(options.RetryInterval)} ({options.RetryInterval}); otherwise the same row can be picked up by the next retry pass before the lock expires.");
            if (options.RetryFailedMax < 5)
                errors.Add($"{nameof(options.RetryFailedMax)} must be >= 5, got {options.RetryFailedMax}");
            if (options.RetryLimitCount <= 0)
                errors.Add($"{nameof(options.RetryLimitCount)} must be > 0, got {options.RetryLimitCount}");
            if (options.RetryMaxDegreeOfParallelism <= 0)
                errors.Add($"{nameof(options.RetryMaxDegreeOfParallelism)} must be > 0, got {options.RetryMaxDegreeOfParallelism}");
            if (options.SucceedExpireHour <= 0)
                errors.Add($"{nameof(options.SucceedExpireHour)} must be > 0, got {options.SucceedExpireHour}");
            if (string.IsNullOrWhiteSpace(options.Environment))
                errors.Add($"{nameof(options.Environment)} must not be empty");

            return errors.Count > 0
                ? ValidateOptionsResult.Fail(errors)
                : ValidateOptionsResult.Success;
        }
    }
}