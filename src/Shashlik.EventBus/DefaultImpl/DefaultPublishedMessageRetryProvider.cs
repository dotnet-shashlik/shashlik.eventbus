using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.DefaultImpl
{
    /// <summary>
    /// 已发送的消息重试提供类
    /// </summary>
    public class DefaultPublishedMessageRetryProvider : IPublishedMessageRetryProvider
    {
        public DefaultPublishedMessageRetryProvider(
            IMessageStorage messageStorage,
            IOptions<EventBusOptions> options,
            IMessageSerializer messageSerializer,
            IPublishHandler publishHandler)
        {
            MessageStorage = messageStorage;
            Options = options;
            MessageSerializer = messageSerializer;
            PublishHandler = publishHandler;
        }

        private IMessageStorage MessageStorage { get; }
        private IOptions<EventBusOptions> Options { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IPublishHandler PublishHandler { get; }

        public async Task StartupAsync(CancellationToken cancellationToken)
        {
            await Retry(cancellationToken).ConfigureAwait(false);

            async void Action() => await Retry(cancellationToken).ConfigureAwait(false);

            TimerHelper.SetInterval(
                Action,
                TimeSpan.FromSeconds(Options.Value.RetryInterval),
                cancellationToken);
        }

        public async Task<HandleResult> RetryAsync(string id, CancellationToken cancellationToken)
        {
            var messageStorageModel =
                await MessageStorage.FindPublishedByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (messageStorageModel is null)
                throw new ArgumentException($"[EventBus]Not found published message of id: {id}", nameof(id));

            var messageTransferModel = new MessageTransferModel
            {
                EventName = messageStorageModel.EventName,
                Environment = messageStorageModel.Environment,
                MsgId = messageStorageModel.MsgId,
                MsgBody = messageStorageModel.EventBody,
                Items = MessageSerializer.Deserialize<IDictionary<string, string>>(messageStorageModel.EventItems),
                SendAt = DateTimeOffset.Now,
                DelayAt = messageStorageModel.DelayAt,
            };

            return await PublishHandler.HandleAsync(messageTransferModel, messageStorageModel, cancellationToken);
        }

        private async Task Retry(CancellationToken cancellationToken)
        {
            var messages = await MessageStorage.GetPublishedMessagesOfNeedRetryAsync(
                Options.Value.RetryLimitCount,
                Options.Value.StartRetryAfter,
                Options.Value.RetryFailedMax,
                Options.Value.Environment,
                cancellationToken).ConfigureAwait(false);
            if (messages.IsNullOrEmpty())
                return;

            Parallel.ForEach(messages, new ParallelOptions
                {
                    MaxDegreeOfParallelism = Options.Value.RetryMaxDegreeOfParallelism
                },
                item =>
                {
                    PublishHandler.LockingHandleAsync(item.Id, cancellationToken).ConfigureAwait(false).GetAwaiter()
                        .GetResult();
                });
        }
    }
}