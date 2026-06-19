using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shashlik.EventBus.Utils
{
    public interface ITimer
    {
        /// <summary>
        ///     在指定时间过后执行指定的表达式, 异步可等待
        /// </summary>
        /// <param name="action">要执行的表达式</param>
        /// <param name="expire">过期时间</param>
        /// <param name="cancellationToken">撤销</param>
        /// <return></return>
        Task SetTimeoutAsync(Func<Task> action, TimeSpan expire,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     在指定时间执行指定的表达式, 异步可等待
        /// </summary>
        /// <param name="action">要执行的表达式</param>
        /// <param name="runAt">过期时间</param>
        /// <param name="cancellationToken">撤销</param>
        /// <return></return>
        Task SetTimeoutAsync(Func<Task> action, DateTimeOffset runAt,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     定时执行任务,不会立即执行
        /// </summary>
        /// <param name="action">要执行的表达式</param>
        /// <param name="interval">间隔时间</param>
        /// <param name="cancellationToken">撤销</param>
        /// <return></return>
        void SetInterval(Action action, TimeSpan interval, CancellationToken cancellationToken = default);

        /// <summary>
        ///     定时执行任务,不会立即执行
        /// </summary>
        /// <param name="action">要执行的表达式</param>
        /// <param name="interval">间隔时间</param>
        /// <param name="cancellationToken">撤销</param>
        /// <return></return>
        void SetInterval(Func<Task> action, TimeSpan interval,
            CancellationToken cancellationToken = default);
    }
}