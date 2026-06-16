using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.DefaultImpl
{
    /// <summary>
    /// 已接收的消息重试提供类
    /// </summary>
    public class DefaultReceivedMessageRetryProvider : IReceivedMessageRetryProvider
    {
        public DefaultReceivedMessageRetryProvider(
            IMessageStorage messageStorage,
            IOptions<EventBusOptions> options,
            ILogger<DefaultReceivedMessageRetryProvider> logger,
            IMessageSerializer messageSerializer,
            IEventHandlerFindProvider eventHandlerFindProvider,
            IReceivedHandler receivedHandler)
        {
            MessageStorage = messageStorage;
            Options = options;
            Logger = logger;
            MessageSerializer = messageSerializer;
            EventHandlerFindProvider = eventHandlerFindProvider;
            ReceivedHandler = receivedHandler;
        }

        private IMessageStorage MessageStorage { get; }
        private IOptions<EventBusOptions> Options { get; }
        private ILogger<DefaultReceivedMessageRetryProvider> Logger { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IEventHandlerFindProvider EventHandlerFindProvider { get; }
        private IReceivedHandler ReceivedHandler { get; }

        public async Task StartupAsync(CancellationToken cancellationToken)
        {
            await Retry(cancellationToken).ConfigureAwait(false);

            // 同步 Action 形态走 TimerHelper,内部 try/catch 不会让异常拖垮 timer。
            // 真实并发由 Retry 内部的 SemaphoreSlim 控。
            TimerHelper.SetInterval(
                () => Retry(cancellationToken).GetAwaiter().GetResult(),
                TimeSpan.FromSeconds(Options.Value.RetryInterval),
                cancellationToken);
        }

        public async Task<HandleResult> RetryAsync(string id, CancellationToken cancellationToken)
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
            // 替换 Parallel.ForEach + GetAwaiter().GetResult,真正以异步方式并发。
            using var semaphore = new SemaphoreSlim(
                Math.Max(1, Options.Value.RetryMaxDegreeOfParallelism));
            var tasks = new List<Task>(messages.Count);
            foreach (var item in messages)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                tasks.Add(RunOneAsync(item, semaphore, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task RunOneAsync(
            MessageStorageModel item,
            SemaphoreSlim semaphore,
            CancellationToken cancellationToken)
        {
            try
            {
                await ReceivedHandler
                    .LockingHandleAsync(item.Id, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    $"[EventBus] retry received message \"{item.Id}\" failed");
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}