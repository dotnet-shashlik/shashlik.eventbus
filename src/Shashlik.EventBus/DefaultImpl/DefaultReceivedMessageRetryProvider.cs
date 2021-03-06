﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shashlik.Utils.Extensions;
using Shashlik.Utils.Helpers;

namespace Shashlik.EventBus.DefaultImpl
{
    /// <summary>
    /// 已接收的消息重试提供类
    /// </summary>
    public class DefaultReceivedMessageRetryProvider : IReceivedMessageRetryProvider
    {
        public DefaultReceivedMessageRetryProvider(IMessageStorage messageStorage,
            IOptions<EventBusOptions> options, ILogger<DefaultPublishedMessageRetryProvider> logger,
            IMessageSerializer messageSerializer, IEventHandlerFindProvider eventHandlerFindProvider,
            IEventHandlerInvoker eventHandlerInvoker)
        {
            MessageStorage = messageStorage;
            Options = options;
            Logger = logger;
            MessageSerializer = messageSerializer;
            EventHandlerFindProvider = eventHandlerFindProvider;
            EventHandlerInvoker = eventHandlerInvoker;
        }

        private IMessageStorage MessageStorage { get; }
        private IOptions<EventBusOptions> Options { get; }
        private ILogger<DefaultPublishedMessageRetryProvider> Logger { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IEventHandlerInvoker EventHandlerInvoker { get; }
        private IEventHandlerFindProvider EventHandlerFindProvider { get; }

        public async Task StartupAsync(CancellationToken cancellationToken)
        {
            await Retry(cancellationToken).ConfigureAwait(false);

            // 重试器执行间隔为5秒
            TimerHelper.SetInterval(
                async () => await Retry(cancellationToken).ConfigureAwait(false),
                TimeSpan.FromSeconds(Options.Value.RetryWorkingIntervalSeconds),
                cancellationToken);
        }

        public async Task RetryAsync(long id, CancellationToken cancellationToken)
        {
            var item = await MessageStorage.FindReceivedByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (item is null)
                throw new ArgumentException($"[EventBus]Not found received message of id: {id}", nameof(id));
            await Retry(item, cancellationToken, false).ConfigureAwait(false);
        }

        private async Task Retry(MessageStorageModel item, CancellationToken cancellationToken, bool checkRetryFailedMax)
        {
            var descriptor = EventHandlerFindProvider.GetByName(item.EventHandlerName);
            if (descriptor is null)
            {
                Logger.LogWarning($"[EventBus] not found of event handler: {item.EventHandlerName}, but receive msg: {item.EventBody}.");
                return;
            }

            if (checkRetryFailedMax && item.RetryCount >= Options.Value.RetryFailedMax)
                return;

            try
            {
                var items = MessageSerializer.Deserialize<IDictionary<string, string>>(item.EventItems);
                Logger.LogDebug(
                    $"[EventBus]Begin execute event handler, event: {item.EventName}, handler: {item.EventHandlerName}, msgId: {item.MsgId}.");
                await EventHandlerInvoker.InvokeAsync(item, items, descriptor).ConfigureAwait(false);
                await MessageStorage.UpdateReceivedAsync(
                        item.Id,
                        MessageStatus.Succeeded,
                        item.RetryCount + 1,
                        DateTime.Now.AddHours(Options.Value.SucceedExpireHour),
                        cancellationToken)
                    .ConfigureAwait(false);

                Logger.LogDebug(
                    $"[EventBus]Execute event handler success, event: {item.EventName}, handler: {item.EventHandlerName}, msgId: {item.MsgId}.");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    $"[EventBus]Received event retry fail, event: {item.EventName}, handler: {item.EventHandlerName}, msgId: {item.MsgId}.");
                try
                {
                    // 失败的数据不过期
                    await MessageStorage.UpdateReceivedAsync(
                            item.Id,
                            MessageStatus.Failed,
                            item.RetryCount + 1,
                            null,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception exInner)
                {
                    Logger.LogError(exInner, $"[EventBus] update received message error.");
                }
            }
        }

        private async Task Retry(CancellationToken cancellationToken)
        {
            // 一次最多读取100条数据
            var messages = await MessageStorage.GetReceivedMessagesOfNeedRetryAndLockAsync(
                Options.Value.RetryLimitCount,
                Options.Value.StartRetryAfterSeconds,
                Options.Value.RetryFailedMax,
                Options.Value.Environment,
                Options.Value.RetryIntervalSeconds,
                cancellationToken).ConfigureAwait(false);
            if (messages.IsNullOrEmpty())
                return;

            // 并行重试
            Parallel.ForEach(
                messages,
                new ParallelOptions {MaxDegreeOfParallelism = Options.Value.RetryMaxDegreeOfParallelism},
                async item => await Retry(item, cancellationToken, true).ConfigureAwait(false)
            );
        }
    }
}