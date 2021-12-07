#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.DefaultImpl
{
    public class DefaultEventPublisher : IEventPublisher
    {
        public DefaultEventPublisher(
            IMessageStorage messageStorage,
            IMessageSerializer messageSerializer,
            IEventNameRuler eventNameRuler,
            IOptions<EventBusOptions> options,
            IMessageSendQueueProvider messageSendQueueProvider, IMsgIdGenerator msgIdGenerator)
        {
            MessageStorage = messageStorage;
            MessageSerializer = messageSerializer;
            EventNameRuler = eventNameRuler;
            Options = options;
            MessageSendQueueProvider = messageSendQueueProvider;
            MsgIdGenerator = msgIdGenerator;
        }

        private IMessageStorage MessageStorage { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IMessageSendQueueProvider MessageSendQueueProvider { get; }
        private IEventNameRuler EventNameRuler { get; }
        private IMsgIdGenerator MsgIdGenerator { get; }
        private IOptions<EventBusOptions> Options { get; }

        public async Task PublishAsync<TEvent>(
            TEvent @event,
            ITransactionContext? transactionContext,
            IDictionary<string, string>? additionalItems = null,
            CancellationToken cancellationToken = default
        ) where TEvent : IEvent
        {
            await Publish(@event, null, transactionContext, additionalItems, cancellationToken).ConfigureAwait(false);
        }

        public async Task PublishAsync<TEvent>(
            TEvent @event,
            DateTimeOffset delayAt,
            ITransactionContext? transactionContext,
            IDictionary<string, string>? additionalItems = null,
            CancellationToken cancellationToken = default
        ) where TEvent : IEvent
        {
            await Publish(@event, delayAt, transactionContext, additionalItems, cancellationToken).ConfigureAwait(false);
        }

        private async Task Publish<TEvent>(
            TEvent @event,
            DateTimeOffset? delayAt,
            ITransactionContext? transactionContext,
            IDictionary<string, string>? additionalItems = null,
            CancellationToken cancellationToken = default
        )
        {
            if (@event is null) throw new ArgumentNullException(nameof(@event));
            if (transactionContext is null)
            {
                try
                {
                    if (Transaction.Current != null)
                        transactionContext = new XaTransactionContext(Transaction.Current);
                }
                catch
                {
                    // ignored
                }
            }

            var now = DateTimeOffset.Now;
            var eventName = EventNameRuler.GetName(typeof(TEvent));
            var msgId = MsgIdGenerator.GenerateId();
            additionalItems ??= new Dictionary<string, string>();
            additionalItems.Add(EventBusConsts.SendAtHeaderKey, now.ToString());
            additionalItems.Add(EventBusConsts.EventNameHeaderKey, eventName);
            additionalItems.Add(EventBusConsts.MsgIdHeaderKey, msgId);
            if (delayAt.HasValue)
            {
                if (delayAt.Value <= DateTimeOffset.Now)
                    delayAt = null;
                else
                    additionalItems.Add(EventBusConsts.DelayAtHeaderKey, delayAt.ToString() ?? "");
            }

            MessageStorageModel messageStorageModel = new MessageStorageModel
            {
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
                EventItems = MessageSerializer.Serialize(additionalItems),
                EventBody = MessageSerializer.Serialize(@event),
                DelayAt = delayAt,
            };

            MessageTransferModel messageTransferModel = new MessageTransferModel
            {
                EventName = messageStorageModel.EventName,
                Environment = Options.Value.Environment,
                MsgId = messageStorageModel.MsgId,
                MsgBody = messageStorageModel.EventBody,
                Items = additionalItems,
                SendAt = now,
                DelayAt = delayAt
            };

            // 消息持久化
            await MessageStorage.SavePublishedAsync(messageStorageModel, transactionContext, cancellationToken).ConfigureAwait(false);
            // 进入消息发送队列
            MessageSendQueueProvider.Enqueue(transactionContext, messageTransferModel, messageStorageModel, cancellationToken);
        }
    }
}