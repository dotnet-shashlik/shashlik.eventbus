// ReSharper disable CheckNamespace

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shashlik.Utils.Extensions;
using Shashlik.Utils.Helpers;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 已发送的消息重试提供类
    /// </summary>
    public class DefaultPublishedMessageRetryProvider : IPublishedMessageRetryProvider
    {
        public DefaultPublishedMessageRetryProvider(IMessageStorage messageStorage, IMessageSender messageSender,
            IOptionsMonitor<EventBusOptions> options, ILogger<DefaultPublishedMessageRetryProvider> logger,
            IMessageSerializer messageSerializer)
        {
            MessageStorage = messageStorage;
            MessageSender = messageSender;
            Options = options;
            Logger = logger;
            MessageSerializer = messageSerializer;
        }

        private IMessageStorage MessageStorage { get; }
        private IMessageSender MessageSender { get; }
        private IOptionsMonitor<EventBusOptions> Options { get; }
        private ILogger<DefaultPublishedMessageRetryProvider> Logger { get; }
        private IMessageSerializer MessageSerializer { get; }

        public void DoRetry()
        {
            TimerHelper.SetInterval(Retry, TimeSpan.FromMinutes(Options.CurrentValue.RetryIntervalSeconds));
        }

        void Retry()
        {
            // 一次最多读取200条数据
            var messages = MessageStorage.GetPublishedMessagesOfNeedRetryAndLock(
                    Options.CurrentValue.RetryLimitCount,
                    Options.CurrentValue.RetryAfterSeconds,
                    Options.CurrentValue.RetryFailedMax, Options.CurrentValue.Environment,
                    Options.CurrentValue.RetryIntervalSeconds)
                .GetAwaiter().GetResult();
            if (messages.IsNullOrEmpty())
                return;

            // 并行重试
            Parallel.ForEach(messages,
                new ParallelOptions {MaxDegreeOfParallelism = Options.CurrentValue.RetryMaxDegreeOfParallelism},
                (item) =>
                {
                    var messageTransferModel = new MessageTransferModel
                    {
                        EventName = item.EventName,
                        MsgId = item.MsgId,
                        MsgBody = MessageSerializer.Serialize(item),
                        SendAt = DateTimeOffset.Now,
                        DelayAt = item.DelayAt
                    };
                    try
                    {
                        MessageSender.Send(messageTransferModel);
                        MessageStorage.UpdatePublished(item.MsgId, MessageStatus.Succeeded, item.RetryCount + 1,
                            DateTime.Now.AddHours(Options.CurrentValue.SucceedExpireHour));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex,
                            $"[EventBus] published event retry fail, event: {item.EventName}, msgId: {item.MsgId}");
                        try
                        {
                            // 失败的数据不过期
                            MessageStorage.UpdatePublished(item.MsgId, MessageStatus.Failed, item.RetryCount + 1,
                                null);
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                });
        }
    }
}