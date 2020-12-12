using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shashlik.Utils.Extensions;
using Shashlik.Utils.Helpers;

namespace Shashlik.EventBus.DefaultImpl
{
    /// <summary>
    /// 已发送的消息重试提供类
    /// </summary>
    public class DefaultPublishedMessageRetryProvider : IPublishedMessageRetryProvider
    {
        public DefaultPublishedMessageRetryProvider(IMessageStorage messageStorage, IMessageSender messageSender,
            IOptions<EventBusOptions> options, ILogger<DefaultPublishedMessageRetryProvider> logger,
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
        private IOptions<EventBusOptions> Options { get; }
        private ILogger<DefaultPublishedMessageRetryProvider> Logger { get; }
        private IMessageSerializer MessageSerializer { get; }

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
            var item = await MessageStorage.FindPublishedByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (item is null)
                throw new ArgumentException($"[EventBus]Not found published message of id: {id}", nameof(id));

            await Retry(item, cancellationToken, false).ConfigureAwait(false);
        }

        private async Task Retry(MessageStorageModel item, CancellationToken cancellationToken, bool checkRetryFailedMax)
        {
            if (checkRetryFailedMax && item.RetryCount >= Options.Value.RetryFailedMax)
                return;

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
                await MessageSender.SendAsync(messageTransferModel).ConfigureAwait(false);
                await MessageStorage.UpdatePublishedAsync(
                        item.Id,
                        MessageStatus.Succeeded,
                        item.RetryCount + 1,
                        DateTime.Now.AddHours(Options.Value.SucceedExpireHour),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    $"[EventBus] published event retry fail, event: {item.EventName}, msgId: {item.MsgId}");
                try
                {
                    // 失败的数据不过期
                    await MessageStorage.UpdatePublishedAsync(
                            item.Id,
                            MessageStatus.Failed,
                            item.RetryCount + 1,
                            null,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception exInner)
                {
                    Logger.LogError(exInner, $"[EventBus] update published message error.");
                }
            }
        }


        private async Task Retry(CancellationToken cancellationToken)
        {
            // 一次最多读取200条数据
            var messages = await MessageStorage.GetPublishedMessagesOfNeedRetryAndLockAsync(
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