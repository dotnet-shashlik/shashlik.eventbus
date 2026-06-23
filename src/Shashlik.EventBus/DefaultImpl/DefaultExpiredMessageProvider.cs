using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.Utils;
using ITimer = Shashlik.EventBus.Utils.ITimer;

// ReSharper disable AsyncVoidLambda

namespace Shashlik.EventBus.DefaultImpl
{
    /// <summary>
    /// 已过期的消息处理
    /// </summary>
    public class DefaultExpiredMessageProvider : IExpiredMessageProvider
    {
        public DefaultExpiredMessageProvider(IMessageStorage messageStorage,
            ILogger<DefaultExpiredMessageProvider> logger, IOptionsMonitor<EventBusOptions> optionsMonitor,
            ITimer timerHelper)
        {
            MessageStorage = messageStorage;
            Logger = logger;
            OptionsMonitor = optionsMonitor;
            TimerHelper = timerHelper;
        }

        private IMessageStorage MessageStorage { get; }
        private ILogger<DefaultExpiredMessageProvider> Logger { get; }
        private IOptionsMonitor<EventBusOptions> OptionsMonitor { get; }
        private ITimer TimerHelper { get; }

        public async Task DoDeleteAsync(CancellationToken cancellationToken)
        {
            await Del(cancellationToken).ConfigureAwait(false);
            // 每个小时执行1次删除
            TimerHelper.SetInterval(
                async () => await Del(cancellationToken).ConfigureAwait(false),
                TimeSpan.FromHours(1),
                cancellationToken);
        }

        private async Task Del(CancellationToken cancellationToken)
        {
            try
            {
                await MessageStorage.DeleteExpiresAsync(OptionsMonitor.CurrentValue.RetryFailedMax, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                //ignore
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"[EventBus] delete expired data occur error");
            }
        }
    }
}