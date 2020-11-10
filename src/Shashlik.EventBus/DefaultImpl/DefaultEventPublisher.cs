#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.DefaultImpl
{
    public class DefaultEventPublisher : IEventPublisher
    {
        public DefaultEventPublisher(
            IMessageStorage messageStorage,
            IMessageSerializer messageSerializer,
            IEventNameRuler eventNameRuler,
            IOptionsMonitor<EventBusOptions> options,
            IMessageSendQueueProvider messageSendQueueProvider)
        {
            MessageStorage = messageStorage;
            MessageSerializer = messageSerializer;
            EventNameRuler = eventNameRuler;
            Options = options;
            MessageSendQueueProvider = messageSendQueueProvider;
        }

        private IMessageStorage MessageStorage { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IMessageSendQueueProvider MessageSendQueueProvider { get; }
        private IEventNameRuler EventNameRuler { get; }
        private IOptionsMonitor<EventBusOptions> Options { get; }

        public async Task PublishAsync<TEvent>(
            TEvent @event,
            TransactionContext? transactionContext,
            IDictionary<string, string>? items = null,
            CancellationToken cancellationToken = default
        ) where TEvent : IEvent
        {
            await Publish(@event, transactionContext, null, items);
        }

        public async Task PublishAsync<TEvent>(
            TEvent @event,
            TransactionContext? transactionContext,
            DateTimeOffset delayAt,
            IDictionary<string, string>? items = null,
            CancellationToken cancellationToken = default
        ) where TEvent : IDelayEvent
        {
            await Publish(@event, transactionContext, delayAt, items);
        }

        private async Task Publish<TEvent>(
            TEvent @event,
            TransactionContext? transactionContext,
            DateTimeOffset? delayAt,
            IDictionary<string, string>? items = null,
            CancellationToken cancellationToken = default
        )
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));
            var now = DateTimeOffset.Now;
            var eventName = EventNameRuler.GetName(typeof(TEvent));
            var msgId = Guid.NewGuid().ToString("n");
            items ??= new Dictionary<string, string>();
            items.Add(EventBusConsts.SendAtHeaderKey, now.ToString());
            items.Add(EventBusConsts.EventNameHeaderKey, eventName);
            items.Add(EventBusConsts.MsgIdHeaderKey, msgId);
            if (delayAt.HasValue)
            {
                if ((delayAt.Value - DateTimeOffset.Now).TotalSeconds < Options.CurrentValue.DelayAtMinSeconds)
                {
                    throw new ArgumentException(
                        $"DelayAt value must great than now {Options.CurrentValue.DelayAtMinSeconds} seconds.",
                        nameof(delayAt));
                }

                items.Add(EventBusConsts.DelayAtHeaderKey, delayAt.ToString());
            }

            MessageStorageModel messageStorageModel = new MessageStorageModel
            {
                MsgId = msgId,
                Environment = Options.CurrentValue.Environment,
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

            MessageTransferModel messageTransferModel = new MessageTransferModel
            {
                EventName = messageStorageModel.EventName,
                MsgId = messageStorageModel.MsgId,
                MsgBody = messageStorageModel.EventBody,
                Items = items,
                SendAt = now,
                DelayAt = delayAt
            };

            // 消息持久化
            await MessageStorage.SavePublished(messageStorageModel, transactionContext, cancellationToken);
            // 进入消息发送队列
            MessageSendQueueProvider.Enqueue(messageTransferModel, messageStorageModel, cancellationToken);
        }
    }
}