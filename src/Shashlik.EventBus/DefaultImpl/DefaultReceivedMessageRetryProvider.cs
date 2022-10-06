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
            IReceivedHandler receivedHandler, IRetryProvider retryProvider)
        {
            MessageStorage = messageStorage;
            Options = options;
            Logger = logger;
            MessageSerializer = messageSerializer;
            EventHandlerFindProvider = eventHandlerFindProvider;
            ReceivedHandler = receivedHandler;
            RetryProvider = retryProvider;
        }

        private IMessageStorage MessageStorage { get; }
        private IOptions<EventBusOptions> Options { get; }
        private ILogger<DefaultReceivedMessageRetryProvider> Logger { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IEventHandlerFindProvider EventHandlerFindProvider { get; }
        private IReceivedHandler ReceivedHandler { get; }
        private IRetryProvider RetryProvider { get; }

        public async Task StartupAsync(CancellationToken cancellationToken)
        {
            await Retry(cancellationToken).ConfigureAwait(false);

            async void Action() => await Retry(cancellationToken).ConfigureAwait(false);
            // 重试器执行间隔为5秒
            TimerHelper.SetInterval(
                Action,
                TimeSpan.FromSeconds(Options.Value.RetryInterval),
                cancellationToken);
        }

        public async Task<HandleResult> RetryAsync(string id, CancellationToken cancellationToken)
        {
            var item = await MessageStorage.FindReceivedByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (item is null)
                throw new ArgumentException($"[EventBus] can not found received message of id: {id}", nameof(id));
            var descriptor = EventHandlerFindProvider.GetByName(item.EventHandlerName);
            if (descriptor is null)
            {
                Logger.LogWarning(
                    $"[EventBus] can not found of event handler: {item.EventHandlerName}, but receive msg: {item.EventBody}");
                return new HandleResult(false, item);
            }

            var items = MessageSerializer.Deserialize<IDictionary<string, string>>(item.EventItems)
                        ?? new Dictionary<string, string>();
            Logger.LogDebug(
                $"[EventBus] begin invoke event handler, event: {item.EventName}, handler: {item.EventHandlerName}, msgId: {item.MsgId}");
            return await ReceivedHandler.HandleAsync(item, items, descriptor, cancellationToken).ConfigureAwait(false);
        }

        private async Task Retry(CancellationToken cancellationToken)
        {
            // 一次最多读取100条数据
            var messages = await MessageStorage.GetReceivedMessagesOfNeedRetryAsync(
                Options.Value.RetryLimitCount,
                Options.Value.StartRetryAfter,
                Options.Value.RetryFailedMax,
                Options.Value.Environment,
                cancellationToken).ConfigureAwait(false);
            if (messages.IsNullOrEmpty())
                return;

            foreach (var item in messages)
            {
                RetryProvider.Retry(item.Id, () => ReceivedHandler.HandleAsync(item.Id, cancellationToken));
            }
        }
    }
}