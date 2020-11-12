// ReSharper disable CheckNamespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shashlik.Utils.Extensions;
using Shashlik.Utils.Helpers;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 已接收的消息重试提供类
    /// </summary>
    public class DefaultReceivedMessageRetryProvider : IReceivedMessageRetryProvider
    {
        public DefaultReceivedMessageRetryProvider(IMessageStorage messageStorage, IMessageSender messageSender,
            IOptionsMonitor<EventBusOptions> options, ILogger<DefaultPublishedMessageRetryProvider> logger,
            IMessageSerializer messageSerializer, IEventHandlerFindProvider eventHandlerFindProvider,
            IEventHandlerInvoker eventHandlerInvoker)
        {
            MessageStorage = messageStorage;
            Options = options;
            Logger = logger;
            MessageSerializer = messageSerializer;
            EventHandlerInvoker = eventHandlerInvoker;
            EventHandlerDescriptors = eventHandlerFindProvider
                .LoadAll()
                .ToDictionary(r => r.EventHandlerName, r => r);
        }

        private IMessageStorage MessageStorage { get; }
        private IOptionsMonitor<EventBusOptions> Options { get; }
        private ILogger<DefaultPublishedMessageRetryProvider> Logger { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IEventHandlerInvoker EventHandlerInvoker { get; }
        private IDictionary<string, EventHandlerDescriptor> EventHandlerDescriptors { get; }

        public async Task DoRetry(CancellationToken cancellationToken)
        {
            await Retry(cancellationToken);

            TimerHelper.SetInterval(async () => await Retry(cancellationToken),
                TimeSpan.FromSeconds(Options.CurrentValue.RetryIntervalSeconds),
                cancellationToken);
        }

        private async Task Retry(CancellationToken cancellationToken)
        {
            // 一次最多读取200条数据
            var messages = await MessageStorage.GetReceivedMessagesOfNeedRetryAndLock(
                Options.CurrentValue.RetryLimitCount,
                Options.CurrentValue.RetryAfterSeconds,
                Options.CurrentValue.RetryFailedMax, Options.CurrentValue.Environment,
                Options.CurrentValue.RetryIntervalSeconds, cancellationToken);
            if (messages.IsNullOrEmpty())
                return;

            // 并行重试
            Parallel.ForEach(messages,
                new ParallelOptions {MaxDegreeOfParallelism = Options.CurrentValue.RetryMaxDegreeOfParallelism},
                async (item) =>
                {
                    if (!EventHandlerDescriptors.TryGetValue(item.EventHandlerName, out var descriptor))
                    {
                        Logger.LogWarning(
                            $"[EventBus] can not find event handler: {item.EventHandlerName}, event: {item.EventName}, msgId: {item.MsgId}");
                        return;
                    }

                    try
                    {
                        var items = (IDictionary<string, string>) MessageSerializer.Deserialize(item.EventItems,
                            typeof(IDictionary<string, string>));

                        EventHandlerInvoker.Invoke(item, items, descriptor);
                        await MessageStorage.UpdateReceived(item.MsgId, MessageStatus.Succeeded, item.RetryCount + 1,
                            DateTime.Now.AddHours(Options.CurrentValue.SucceedExpireHour), cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex,
                            $"[EventBus] received event retry fail, event: {item.EventName}, msgId: {item.MsgId}");
                        try
                        {
                            // 失败的数据不过期
                            await MessageStorage.UpdateReceived(item.MsgId, MessageStatus.Failed, item.RetryCount + 1,
                                null, cancellationToken);
                        }
                        catch (Exception exInner)
                        {
                            Logger.LogError($"[EventBus] update received message error.", exInner);
                        }
                    }
                });
        }
    }
}