using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
        /// <param name="onError">action 抛异常时的回调,默认 Console.WriteLine</param>
        /// <return></return>
        public static void SetTimeout(
            Action action,
            TimeSpan expire,
            CancellationToken cancellationToken = default,
            Action<Exception>? onError = null)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            if (expire <= TimeSpan.Zero)
                throw new ArgumentException("invalid expire.", nameof(expire));

            Task.Run(async () =>
            {
                using var timer = new PeriodicTimer(expire);
                try
                {
                    if (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                    {
                        SafeInvoke(action, onError);
                    }
                }
                catch (OperationCanceledException)
                {
                    // 正常取消
                }
                catch (Exception ex)
                {
                    SafeReport(ex, onError);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// 在指定时间执行指定的表达式
        /// </summary>
        public static void SetTimeout(
            Action action,
            DateTimeOffset runAt,
            CancellationToken cancellationToken = default,
            Action<Exception>? onError = null)
        {
            SetTimeout(action, (runAt - DateTimeOffset.Now), cancellationToken, onError);
        }

        /// <summary>
        /// 定时执行任务,不会立即执行
        /// </summary>
        /// <param name="action">要执行的表达式</param>
        /// <param name="interval">间隔时间</param>
        /// <param name="cancellationToken">撤销</param>
        /// <param name="onError">action 抛异常时的回调,默认 Console.WriteLine</param>
        public static void SetInterval(
            Action action,
            TimeSpan interval,
            CancellationToken cancellationToken = default,
            Action<Exception>? onError = null)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            if (interval <= TimeSpan.Zero)
                throw new ArgumentException("invalid interval.", nameof(interval));

            Task.Run(async () =>
            {
                using var timer = new PeriodicTimer(interval);
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        if (!await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                            break;
                        SafeInvoke(action, onError);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        // 单次失败不应停止整个定时循环;打 error 然后继续下一 tick。
                        SafeReport(ex, onError);
                    }
                }
            }, cancellationToken);
        }

        private static void SafeInvoke(Action action, Action<Exception>? onError)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                SafeReport(ex, onError);
            }
        }

        private static void SafeReport(Exception ex, Action<Exception>? onError)
        {
            if (onError is not null)
            {
                try { onError(ex); }
                catch { /* 吞掉 callback 自身的异常 */ }
            }
            else
            {
                Console.WriteLine($"[EventBus] TimerHelper action threw: {ex}");
            }
        }
    }
}