using System;
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
            await Retry(cancellationToken).ConfigureAwait(false);

            // 重试器执行间隔为5秒
            TimerHelper.SetInterval(
                async () => await Retry(cancellationToken).ConfigureAwait(false),
                TimeSpan.FromSeconds(Options.CurrentValue.RetryWorkingIntervalSeconds),
                cancellationToken);
        }

        private async Task Retry(CancellationToken cancellationToken)
        {
            // 一次最多读取100条数据
            var messages = await MessageStorage.GetReceivedMessagesOfNeedRetryAndLock(
                Options.CurrentValue.RetryLimitCount,
                Options.CurrentValue.StartRetryAfterSeconds,
                Options.CurrentValue.RetryFailedMax,
                Options.CurrentValue.Environment,
                Options.CurrentValue.RetryIntervalSeconds,
                cancellationToken).ConfigureAwait(false);
            if (messages.IsNullOrEmpty())
                return;

            Logger.LogDebug(
                $"[EventBus] find need retry received {messages.Count()} message, will do retry.");

            // 并行重试
            Parallel.ForEach(messages, new ParallelOptions {MaxDegreeOfParallelism = Options.CurrentValue.RetryMaxDegreeOfParallelism},
                async (item) =>
                {
                    if (!EventHandlerDescriptors.TryGetValue(item.EventHandlerName, out var descriptor))
                    {
                        Logger.LogWarning(
                            $"[EventBus] can not find event handler: {item.EventHandlerName}, event: {item.EventName}, msgId: {item.MsgId}.");
                        return;
                    }

                    try
                    {
                        var items = MessageSerializer.Deserialize<IDictionary<string, string>>(item.EventItems);
                        Logger.LogDebug(
                            $"[EventBus] begin execute event handler, event: {item.EventName}, handler: {item.EventHandlerName}, msgId: {item.MsgId}.");
                        await EventHandlerInvoker.Invoke(item, items, descriptor).ConfigureAwait(false);
                        await MessageStorage.UpdateReceived(
                                item.Id,
                                MessageStatus.Succeeded,
                                item.RetryCount + 1,
                                DateTime.Now.AddHours(Options.CurrentValue.SucceedExpireHour),
                                cancellationToken)
                            .ConfigureAwait(false);

                        Logger.LogDebug(
                            $"[EventBus] execute event handler success, event: {item.EventName}, handler: {item.EventHandlerName}, msgId: {item.MsgId}.");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex,
                            $"[EventBus] received event retry fail, event: {item.EventName}, handler: {item.EventHandlerName}, msgId: {item.MsgId}.");
                        try
                        {
                            // 失败的数据不过期
                            await MessageStorage.UpdateReceived(
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
                });
        }
    }
}