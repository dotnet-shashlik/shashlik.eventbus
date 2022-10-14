using System;
using System.Threading;
using Timer = System.Timers.Timer;

namespace Shashlik.EventBus.Utils
{
    public static class TimerHelper
    {
        /// <summary>
        /// 在指定时间过后执行指定的表达式
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

            var timer = new Timer { Interval = expire.TotalMilliseconds, AutoReset = false };
            timer.Elapsed += (_, _) =>
            {
                try
                {
                    using (timer)
                    {
                        if (!cancellationToken.IsCancellationRequested)
                            action();

                        timer.Stop();
                        timer.Close();
                    }
                }
                catch
                {
                    // ignore
                }
            };
            timer.Start();
        }

        /// <summary>
        /// 在指定时间执行指定的表达式
        /// </summary>
        /// <param name="action">要执行的表达式</param>
        /// <param name="runAt">过期时间</param>
        /// <param name="cancellationToken">撤销</param>
        /// <return></return>
        public static void SetTimeout(Action action, DateTimeOffset runAt,
            CancellationToken cancellationToken = default)
        {
            SetTimeout(action, (runAt - DateTimeOffset.Now), cancellationToken);
        }

        /// <summary>
        /// 定时执行任务,不会立即执行
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

            var timer = new Timer { Interval = interval.TotalMilliseconds, AutoReset = true };
            timer.Elapsed += (_, _) =>
            {
                if (!cancellationToken.IsCancellationRequested)
                    action();
                else
                    try
                    {
                        using (timer)
                        {
                            timer.Stop();
                            timer.Close();
                        }
                    }
                    catch
                    {
                        // ignored
                    }
            };
            timer.Start();
        }
    }
}