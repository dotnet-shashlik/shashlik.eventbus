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
    /// 已发送的消息重试提供类
    /// </summary>
    public class DefaultPublishedMessageRetryProvider : IPublishedMessageRetryProvider, IDisposable
    {
        public DefaultPublishedMessageRetryProvider(
            IMessageStorage messageStorage,
            IOptions<EventBusOptions> options,
            IMessageSerializer messageSerializer,
            IPublishHandler publishHandler, ITimer timerHelper, ILogger<DefaultPublishedMessageRetryProvider> logger)
        {
            MessageStorage = messageStorage;
            Options = options;
            MessageSerializer = messageSerializer;
            PublishHandler = publishHandler;
            TimerHelper = timerHelper;
            Logger = logger;
        }

        private IMessageStorage MessageStorage { get; }
        private IOptions<EventBusOptions> Options { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IPublishHandler PublishHandler { get; }
        private ILogger<DefaultPublishedMessageRetryProvider> Logger { get; }
        private ITimer TimerHelper { get; }
        private CancellationTokenSource? _internalTask;

        public async Task StartupAsync(CancellationToken cancellationToken)
        {
            await Retry(cancellationToken).ConfigureAwait(false);

            _internalTask = TimerHelper.SetInterval(
                () => Retry(cancellationToken),
                TimeSpan.FromSeconds(Options.Value.RetryInterval),
                cancellationToken);
        }

        public async Task<HandleResult> RetryAsync(long id, CancellationToken cancellationToken)
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
                Items = MessageSerializer.Deserialize<IDictionary<string, string>>(messageStorageModel.EventItems!),
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
                await PublishHandler
                    .LockingHandleAsync(item.Id, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                //ignore
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"[EventBus] Retry message `{item.Id}` occur error: ");
            }
            finally
            {
                semaphore.Release();
            }
        }

        public void Dispose()
        {
            _internalTask?.Dispose();
        }
    }
}