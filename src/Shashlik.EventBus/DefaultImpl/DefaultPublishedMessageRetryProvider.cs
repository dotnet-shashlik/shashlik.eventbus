using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
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

            // 之前 async void Action() 是 TimerHelper 唯一接受的 Action 形态,所以被迫
            // 退化为 async void(异常无人观察)。这里改成同步 Action,在 Retry 内部
            // 用 SemaphoreSlim 控并发;Retry 内部 try/catch 兜底,不会拖垮整个循环。
            TimerHelper.SetInterval(
                () => Retry(cancellationToken).GetAwaiter().GetResult(),
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

            // 之前用 Parallel.ForEach + .GetAwaiter().GetResult() 同步阻塞线程池线程,
            // 在 ASP.NET Core 上抢占请求线程,并发度被 RetryMaxDegreeOfParallelism 卡死
            // 但每次仍以同步方式占用一个线程直到下游 publish 完成。改用 SemaphoreSlim
            // + Task.WhenAll 真正以异步方式并发,单次 batch 的线程占用降到 1。
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
            catch (Exception ex)
            {
                // 单条失败不应影响批内其他任务。
                System.Console.WriteLine(ex);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}