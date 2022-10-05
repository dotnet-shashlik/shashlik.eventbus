using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shashlik.EventBus.Utils;

// ReSharper disable AsyncVoidLambda

namespace Shashlik.EventBus.DefaultImpl
{
    /// <summary>
    /// 已过期的消息处理
    /// </summary>
    public class DefaultExpiredMessageProvider : IExpiredMessageProvider
    {
        public DefaultExpiredMessageProvider(IMessageStorage messageStorage,
            ILogger<DefaultExpiredMessageProvider> logger)
        {
            MessageStorage = messageStorage;
            Logger = logger;
        }

        private IMessageStorage MessageStorage { get; }
        private ILogger<DefaultExpiredMessageProvider> Logger { get; }

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
                await MessageStorage.DeleteExpiresAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"[EventBus] delete expired data occur error");
            }
        }
    }
}