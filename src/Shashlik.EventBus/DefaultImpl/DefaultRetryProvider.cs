using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.DefaultImpl
{
    internal class DefaultRetryProvider : IRetryProvider
    {
        private IOptions<EventBusOptions> Options { get; }
        private ConcurrentDictionary<string, Func<Task<HandleResult>>> Tasks { get; }

        public DefaultRetryProvider(IOptions<EventBusOptions> options)
        {
            Options = options;
            Tasks = new ConcurrentDictionary<string, Func<Task<HandleResult>>>();
        }

        public void Retry(string storageId, Func<Task<HandleResult>> retryAction)
        {
            if (!Tasks.TryAdd(storageId, retryAction))
                return;

            var source = new CancellationTokenSource();
            source.Token.Register(() => Tasks.TryRemove(storageId, out _));
            source.Token.Register(() => source.Dispose());

            async void Action()
            {
                if (source.IsCancellationRequested)
                    return;
                var res = await retryAction().ConfigureAwait(false);
                if (res.Success || res.MessageStorageModel!.RetryCount >= Options.Value.RetryFailedMax) source.Cancel();
            }

            // TimerHelper不会启动时就执行,一开始就执行一次
            Action();
            if (!source.IsCancellationRequested)
            {
                TimerHelper.SetInterval(
                    Action,
                    TimeSpan.FromSeconds(Options.Value.RetryInterval),
                    source.Token);
            }
        }
    }
}