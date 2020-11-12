using System;
using System.Threading;

namespace Shashlik.EventBus.DefaultImpl
{
    public class DefaultMessageListener : IMessageListener
    {
        public DefaultMessageListener(
            EventHandlerDescriptor descriptor,
            string environment,
            IMessageSerializer messageSerializer,
            IMessageStorage messageStorage,
            IMessageReceiveQueueProvider messageReceiveQueueProvider,
            IReceivedDelayEventProvider receivedDelayEventProvider)
        {
            Descriptor = descriptor;
            Environment = environment;
            MessageSerializer = messageSerializer;
            MessageStorage = messageStorage;
            MessageReceiveQueueProvider = messageReceiveQueueProvider;
            ReceivedDelayEventProvider = receivedDelayEventProvider;
        }

        public EventHandlerDescriptor Descriptor { get; }
        private string Environment { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IMessageStorage MessageStorage { get; }
        private IMessageReceiveQueueProvider MessageReceiveQueueProvider { get; }
        private IReceivedDelayEventProvider ReceivedDelayEventProvider { get; }

        public void OnReceive(MessageTransferModel message, CancellationToken cancellationToken)
        {
            var now = DateTime.Now;
            if (message.Items == null) return;
            var receiveMessageStorageModel = new MessageStorageModel
            {
                MsgId = message.MsgId,
                Environment = Environment,
                CreateTime = now,
                ExpireTime = null,
                EventHandlerName = Descriptor.EventHandlerName,
                EventName = message.EventName,
                RetryCount = 0,
                Status = MessageStatus.Scheduled,
                IsLocking = false,
                LockEnd = null,
                EventItems = MessageSerializer.Serialize(message.Items),
                EventBody = message.MsgBody,
                DelayAt = message.DelayAt
            };

            // 消息id已经存在不再处理
            if (MessageStorage.ExistsReceiveMessage(message.MsgId, cancellationToken).GetAwaiter().GetResult())
                return;

            // 保存接收到的消息
            MessageStorage.SaveReceived(receiveMessageStorageModel, cancellationToken).GetAwaiter().GetResult();

            // 非延迟事件直接进入执行队列
            if (!message.DelayAt.HasValue)
                // 进入接收消息处理队列
                MessageReceiveQueueProvider.Enqueue(receiveMessageStorageModel, message.Items, Descriptor,
                    cancellationToken);
            // 延迟事件进入延迟执行队列
            else
                ReceivedDelayEventProvider.Enqueue(receiveMessageStorageModel, message.Items, Descriptor,
                    cancellationToken);
        }
    }
}