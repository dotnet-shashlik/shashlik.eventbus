using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

// ReSharper disable MethodSupportsCancellation

namespace Shashlik.EventBus.DefaultImpl
{
    public class DefaultEventPublisher : IEventPublisher
    {
        public DefaultEventPublisher(
            IMessageStorage messageStorage,
            IMessageSerializer messageSerializer,
            IEventNameRuler eventNameRuler,
            IOptions<EventBusOptions> options,
            IMsgIdGenerator msgIdGenerator,
            IPublishHandler publishHandler,
            IHostedStopToken hostedStopToken,
            ILogger<DefaultEventPublisher> logger, IIdGenerator idGenerator)
        {
            MessageStorage = messageStorage;
            MessageSerializer = messageSerializer;
            EventNameRuler = eventNameRuler;
            Options = options;
            MsgIdGenerator = msgIdGenerator;
            PublishHandler = publishHandler;
            HostedStopToken = hostedStopToken;
            Logger = logger;
            IdGenerator = idGenerator;
        }

        private IMessageStorage MessageStorage { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IEventNameRuler EventNameRuler { get; }
        private IMsgIdGenerator MsgIdGenerator { get; }
        private IOptions<EventBusOptions> Options { get; }
        private IPublishHandler PublishHandler { get; }
        private IHostedStopToken HostedStopToken { get; }
        private ILogger<DefaultEventPublisher> Logger { get; }
        private IIdGenerator IdGenerator { get; }

        public async Task PublishAsync<TEvent>(
            TEvent @event,
            ITransactionContext? transactionContext,
            IDictionary<string, string>? additionalItems = null,
            CancellationToken cancellationToken = default
        ) where TEvent : IEvent
        {
            await InnerPublish(@event, null, transactionContext, additionalItems, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task PublishAsync<TEvent>(
            TEvent @event,
            DateTimeOffset delayAt,
            ITransactionContext? transactionContext,
            IDictionary<string, string>? additionalItems = null,
            CancellationToken cancellationToken = default
        ) where TEvent : IEvent
        {
            await InnerPublish(@event, delayAt, transactionContext, additionalItems, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task InnerPublish<TEvent>(
            TEvent @event,
            DateTimeOffset? delayAt,
            ITransactionContext? transactionContext,
            IDictionary<string, string>? additionalItems = null,
            CancellationToken cancellationToken = default
        )
        {
            if (@event is null) throw new ArgumentNullException(nameof(@event));

            var now = DateTimeOffset.Now;
            var eventName = EventNameRuler.GetName(typeof(TEvent));
            var msgId = MsgIdGenerator.GenerateId();
            // 复制到本地字典,避免修改调用方传入的 items
            // 同时防止重发时 items 里已有 eventbus-* key 导致 Add 抛 ArgumentException
            var items = additionalItems is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(additionalItems);
            items[EventBusConsts.SendAtHeaderKey] = now.ToString();
            items[EventBusConsts.EventNameHeaderKey] = eventName;
            items[EventBusConsts.MsgIdHeaderKey] = msgId;
            if (delayAt.HasValue)
            {
                if (delayAt.Value <= DateTimeOffset.Now)
                    delayAt = null;
                else
                    items[EventBusConsts.DelayAtHeaderKey] = delayAt.ToString() ?? "";
            }

            MessageStorageModel messageStorageModel = new()
            {
                Id = IdGenerator.NextId(),
                MsgId = msgId,
                Environment = Options.Value.Environment,
                CreateTime = now,
                ExpireTime = null,
                EventHandlerName = null,
                EventName = EventNameRuler.GetName(typeof(TEvent)),
                RetryCount = 0,
                Status = MessageStatus.Scheduled,
                IsLocking = false,
                LockEnd = null,
                EventItems = MessageSerializer.Serialize(items),
                EventBody = MessageSerializer.Serialize(@event),
                DelayAt = delayAt,
            };

            MessageTransferModel messageTransferModel = new()
            {
                EventName = messageStorageModel.EventName,
                Environment = Options.Value.Environment,
                MsgId = messageStorageModel.MsgId,
                MsgBody = messageStorageModel.EventBody,
                Items = items,
                SendAt = now,
                DelayAt = delayAt
            };

            // 消息持久化
            await MessageStorage.SavePublishedAsync(messageStorageModel, transactionContext, cancellationToken)
                .ConfigureAwait(false);
            // 先持久化,持久化没有错误,异步发送消息
            // 异步发送消息,启动时如果失败,最多循环5次
            // 用 IHostedStopToken 而非调用方传进来的 cancellationToken —— 否则 HTTP 请求结束
            // 就会取消发布任务,导致"持久化成功但未发送",只能等 StartRetryAfter 后重试器兜底。
            // 持久化 + 事务已经保证调用方有理由期待这条消息一定会被发送,发送的取消只应该由
            // 应用关停触发。
            _ = Task.Run(
                async () => await Start(transactionContext, messageStorageModel, messageTransferModel,
                        HostedStopToken.StopCancellationToken)
                    .ConfigureAwait(false)
                , HostedStopToken.StopCancellationToken).ConfigureAwait(false);
        }

        private async Task Start(
            ITransactionContext? transactionContext,
            MessageStorageModel messageStorageModel,
            MessageTransferModel messageTransferModel,
            CancellationToken cancellationToken)
        {
            // 等待事务完成。TransactionCommitTimeout 是"事务提交等待"超时。
            var now = DateTimeOffset.Now;
            while (!cancellationToken.IsCancellationRequested && transactionContext != null &&
                   !transactionContext.IsDone())
            {
                if ((DateTimeOffset.Now - now).TotalSeconds > Options.Value.TransactionCommitTimeout)
                {
                    Logger.LogDebug($"[EventBus] message \"{messageStorageModel}\" transaction commit timeout");
                    return;
                }

                await Task.Delay(10).ConfigureAwait(false);
            }

            // 事务提交了,判断消息数据是否已提交
            try
            {
                // 消息未提交, 不执行任何操作
                if (!await MessageStorage
                        .IsCommittedAsync(messageStorageModel.MsgId, cancellationToken)
                        .ConfigureAwait(false))
                {
                    Logger.LogDebug($"[EventBus] message \"{messageStorageModel.Id}\" has been rollback");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, $"[EventBus] query message \"{messageStorageModel.Id}\" commit state occur error");
                // 查询异常，将由重试器处理
                return;
            }

            // 执行失败的次数
            // 之前同时复用 TransactionCommitTimeout 作为"消息存活"上限,导致事务耗时 50s
            // 时发布路径只剩 10s 发送,失败率被人为放大。这里只用 5 次硬上限 + 自然退出
            // (cancellationToken) 来限定在进程生命周期内的尝试次数;真正的"重发兜底"由
            // IPublishedMessageRetryProvider 在 StartRetryAfter 之后接手。
            var failCount = 1;
            while (!cancellationToken.IsCancellationRequested)
            {
                // 执行真正的消息发送
                var handleResult = await PublishHandler
                    .HandleAsync(messageTransferModel, messageStorageModel, cancellationToken)
                    .ConfigureAwait(false);
                if (!handleResult.Success)
                    failCount++;
                else
                    return;

                if (failCount > 5)
                {
                    // 将由重试器处理,为了减少线程消耗这里不再精准执行
                    return;
                }
            }
        }
    }
}