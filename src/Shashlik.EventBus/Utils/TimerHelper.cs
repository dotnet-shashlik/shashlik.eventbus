using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shashlik.EventBus.Utils
{
    public static class TimerHelper
    {
        /// <summary>
        ///     在指定时间过后执行指定的表达式
        /// </summary>
        /// <param name="action">要执行的表达式</param>
        /// <param name="expire">过期时间</param>
        /// <param name="cancellationToken">撤销</param>
        /// <return></return>
        public static void SetTimeout(Action action, TimeSpan expire, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            if (expire <= TimeSpan.Zero)
                throw new ArgumentException("invalid expire.", nameof(expire));

            _ = Task.Delay(expire, cancellationToken)
                .ContinueWith(_ => action(), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        ///     在指定时间过后执行指定的表达式
        /// </summary>
        /// <param name="action">要执行的表达式</param>
        /// <param name="expire">过期时间</param>
        /// <param name="cancellationToken">撤销</param>
        /// <return></return>
        public static void SetTimeout(Func<Task> action, TimeSpan expire, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            if (expire <= TimeSpan.Zero)
                throw new ArgumentException("invalid expire.", nameof(expire));

            _ = Task.Delay(expire, cancellationToken)
                .ContinueWith(async _ => await action(), cancellationToken);
        }

        /// <summary>
        ///     在指定时间执行指定的表达式
        /// </summary>
        /// <param name="action">要执行的表达式</param>
        /// <param name="runAt">过期时间</param>
        /// <param name="cancellationToken">撤销</param>
        /// <return></return>
        public static void SetTimeout(Action action, DateTimeOffset runAt,
            CancellationToken cancellationToken = default)
        {
            SetTimeout(action, runAt - DateTimeOffset.Now, cancellationToken);
        }

        /// <summary>
        ///     在指定时间执行指定的表达式
        /// </summary>
        /// <param name="action">要执行的表达式</param>
        /// <param name="runAt">过期时间</param>
        /// <param name="cancellationToken">撤销</param>
        /// <return></return>
        public static void SetTimeout(Func<Task> action, DateTimeOffset runAt,
            CancellationToken cancellationToken = default)
        {
            SetTimeout(action, runAt - DateTimeOffset.Now, cancellationToken);
        }

        /// <summary>
        ///     定时执行任务,不会立即执行
        /// </summary>
        /// <param name="action">要执行的表达式</param>
        /// <param name="interval">间隔时间</param>
        /// <param name="cancellationToken">撤销</param>
        /// <return></return>
        public static void SetInterval(Action action, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            if (interval <= TimeSpan.Zero)
                throw new ArgumentException("invalid interval.", nameof(interval));
            _ = Task.Run(async () =>
            {
                using var timer = new PeriodicTimer(interval);
                while (await timer.WaitForNextTickAsync(cancellationToken))
                    action();
            }, cancellationToken);
        }

        /// <summary>
        ///     定时执行任务,不会立即执行
        /// </summary>
        /// <param name="action">要执行的表达式</param>
        /// <param name="interval">间隔时间</param>
        /// <param name="cancellationToken">撤销</param>
        /// <return></return>
        public static void SetInterval(Func<Task> action, TimeSpan interval,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            if (interval <= TimeSpan.Zero)
                throw new ArgumentException("invalid interval.", nameof(interval));
            _ = Task.Run(async () =>
            {
                using var timer = new PeriodicTimer(interval);
                while (await timer.WaitForNextTickAsync(cancellationToken))
                    await action();
            }, cancellationToken);
        }
    }
}