using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ITimer = Shashlik.EventBus.Utils.ITimer;

namespace Shashlik.EventBus.DefaultImpl;

public class DefaultTimer(ILogger<DefaultTimer> logger) : ITimer
{
    /// <summary>
    ///     在指定时间过后执行指定的表达式, 异步可等待
    /// </summary>
    /// <param name="action">要执行的表达式</param>
    /// <param name="expire">过期时间</param>
    /// <param name="cancellationToken">撤销</param>
    /// <return></return>
    public async Task SetTimeoutAsync(Func<Task> action, TimeSpan expire,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        if (expire <= TimeSpan.Zero)
            throw new ArgumentException("invalid expire.", nameof(expire));

        await Task.Delay(expire, cancellationToken).ConfigureAwait(false);
        await action().ConfigureAwait(false);
    }

    /// <summary>
    ///     在指定时间执行指定的表达式, 异步可等待
    /// </summary>
    /// <param name="action">要执行的表达式</param>
    /// <param name="runAt">过期时间</param>
    /// <param name="cancellationToken">撤销</param>
    /// <return></return>
    public Task SetTimeoutAsync(Func<Task> action, DateTimeOffset runAt,
        CancellationToken cancellationToken = default)
    {
        return SetTimeoutAsync(action, runAt.ToUniversalTime() - DateTimeOffset.UtcNow, cancellationToken);
    }

    /// <summary>
    ///     定时执行任务,不会立即执行
    /// </summary>
    /// <param name="action">要执行的表达式</param>
    /// <param name="interval">间隔时间</param>
    /// <param name="cancellationToken">撤销</param>
    /// <return></return>
    public void SetInterval(Action action, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return;
        if (interval <= TimeSpan.Zero)
            throw new ArgumentException("invalid interval.", nameof(interval));
        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(interval);
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    action();
                }
                catch (OperationCanceledException)
                {
                    //ignore
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"SetInterval execute occur error");
                }
            }
        }, cancellationToken);
    }

    /// <summary>
    ///     定时执行任务,不会立即执行
    /// </summary>
    /// <param name="action">要执行的表达式</param>
    /// <param name="interval">间隔时间</param>
    /// <param name="cancellationToken">撤销</param>
    /// <return></return>
    public void SetInterval(Func<Task> action, TimeSpan interval,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return;
        if (interval <= TimeSpan.Zero)
            throw new ArgumentException("invalid interval.", nameof(interval));
        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(interval);
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    await action();
                }
                catch (OperationCanceledException)
                {
                    //ignore
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"SetInterval execute occur error");
                }
            }
        }, cancellationToken);
    }
}