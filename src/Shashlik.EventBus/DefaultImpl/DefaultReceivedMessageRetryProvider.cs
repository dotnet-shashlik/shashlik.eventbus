using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.Utils;
using ITimer = Shashlik.EventBus.Utils.ITimer;

namespace Shashlik.EventBus.DefaultImpl
{
    /// <summary>
    /// 已接收的消息重试提供类
    /// </summary>
    public class DefaultReceivedMessageRetryProvider : IReceivedMessageRetryProvider, IDisposable
    {
        public DefaultReceivedMessageRetryProvider(
            IMessageStorage messageStorage,
            IOptions<EventBusOptions> options,
            ILogger<DefaultReceivedMessageRetryProvider> logger,
            IMessageSerializer messageSerializer,
            IEventHandlerFindProvider eventHandlerFindProvider,
            IReceivedHandler receivedHandler, ITimer timerHelper)
        {
            MessageStorage = messageStorage;
            Options = options;
            Logger = logger;
            MessageSerializer = messageSerializer;
            EventHandlerFindProvider = eventHandlerFindProvider;
            ReceivedHandler = receivedHandler;
            TimerHelper = timerHelper;
        }

        private IMessageStorage MessageStorage { get; }
        private IOptions<EventBusOptions> Options { get; }
        private ILogger<DefaultReceivedMessageRetryProvider> Logger { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IEventHandlerFindProvider EventHandlerFindProvider { get; }
        private IReceivedHandler ReceivedHandler { get; }
        private ITimer TimerHelper { get; }
        private CancellationTokenSource? _internalTask;

        public async Task StartupAsync(CancellationToken cancellationToken)
        {
            await Retry(cancellationToken).ConfigureAwait(false);

            // 同步 Action 形态走 TimerHelper,内部 try/catch 不会让异常拖垮 timer。
            // 真实并发由 Retry 内部的 SemaphoreSlim 控。
            _internalTask = TimerHelper.SetInterval(
                async () => await Retry(cancellationToken),
                TimeSpan.FromSeconds(Options.Value.RetryInterval),
                cancellationToken);
        }

        public async Task<HandleResult> RetryAsync(long id, CancellationToken cancellationToken)
        {
            var item = await MessageStorage.FindReceivedByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (item is null)
                throw new ArgumentException($"[EventBus] can not found received message of id: {id}", nameof(id));
            var descriptor = EventHandlerFindProvider.GetByName(item.EventHandlerName!);
            if (descriptor is null)
            {
                Logger.LogWarning(
                    $"[EventBus] can not found of event handler: {item.EventHandlerName}, but receive msg: {item.EventBody}");
                return new HandleResult(false, item);
            }

            var items = MessageSerializer.Deserialize<IDictionary<string, string>>(item.EventItems!)
                        ?? new Dictionary<string, string>();
            Logger.LogDebug(
                $"[EventBus] begin invoke event handler, event: {item.EventName}, handler: {item.EventHandlerName}, msgId: {item.MsgId}");
            return await ReceivedHandler.HandleAsync(item, items, descriptor, cancellationToken).ConfigureAwait(false);
        }

        private async Task Retry(CancellationToken cancellationToken)
        {
            // 一次最多读取 Options.RetryLimitCount 条
            var messages = await MessageStorage.GetReceivedMessagesOfNeedRetryAsync(
                Options.Value.RetryLimitCount,
                Options.Value.StartRetryAfter,
                Options.Value.RetryFailedMax,
                Options.Value.Environment,
                cancellationToken).ConfigureAwait(false);
            if (messages.IsNullOrEmpty())
                return;

            // 见 DefaultPublishedMessageRetryProvider: 用 SemaphoreSlim + Task.WhenAll
            await Parallel.ForEachAsync(messages, new ParallelOptions
            {
                MaxDegreeOfParallelism = Options.Value.RetryMaxDegreeOfParallelism,
                CancellationToken = cancellationToken
            }, async (item, cToken) =>
            {
                if (!item.DelayAt.HasValue
                    || item.DelayAt.Value <= DateTimeOffset.Now
                    // 在容忍延迟范围内
                    || (item.DelayAt.Value - DateTimeOffset.Now).TotalSeconds <=
                    Options.Value.DelayedMessageToleranceSeconds
                   )
                {
                    await RunNowAsync(item, cToken).ConfigureAwait(false);
                }
                else
                {
                    await RunDelayAsync(item, cToken).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
        }

        private async Task RunNowAsync(
            MessageStorageModel item,
            CancellationToken cancellationToken)
        {
            try
            {
                await ReceivedHandler
                    .LockAndHandleAsync(item.Id, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.LogError(ex,
                    $"[EventBus] retry received message \"{item.Id}\" failed");
            }
        }

        private async Task RunDelayAsync(
            MessageStorageModel item,
            CancellationToken cancellationToken)
        {
            try
            {
                var lockEndAt = item.DelayAt!.Value.AddSeconds(Options.Value.LockTime);
                var lockRes = await ReceivedHandler
                    .LockAsync(item.Id, lockEndAt, cancellationToken)
                    .ConfigureAwait(false);
                if (lockRes)
                {
                    _ = TimerHelper.SetTimeoutAsync(
                        async () =>
                        {
                            {
                                try
                                {
                                    await ReceivedHandler.HandleAsync(item.Id, cancellationToken);
                                }
                                catch (Exception e) when (e is not OperationCanceledException)
                                {
                                    Logger.LogError(e, $"[EventBus] retry received delay message \"{item.Id}\" failed");
                                }
                            }
                        },
                        item.DelayAt.Value, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    $"[EventBus] retry received delay message \"{item.Id}\" failed");
            }
        }

        public void Dispose()
        {
            _internalTask?.Dispose();
        }
    }
}