using System;

namespace Shashlik.EventBus.DefaultImpl
{
    public class DefaultMessageListener : IMessageListener
    {
        public DefaultMessageListener(
            EventHandlerDescriptor descriptor,
            string environment,
            IMessageSerializer messageSerializer,
            IMessageStorage messageStorage,
            IMessageReceiveQueueProvider messageReceiveQueueProvider)
        {
            Descriptor = descriptor;
            Environment = environment;
            MessageSerializer = messageSerializer;
            MessageStorage = messageStorage;
            MessageReceiveQueueProvider = messageReceiveQueueProvider;
        }

        public EventHandlerDescriptor Descriptor { get; }
        private string Environment { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IMessageStorage MessageStorage { get; }
        private IMessageReceiveQueueProvider MessageReceiveQueueProvider { get; }

        public void Receive(MessageTransferModel message)
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
            if (MessageStorage.ExistsReceiveMessage(message.MsgId).GetAwaiter().GetResult())
                return;

            // 保存接收到的消息
            MessageStorage.SaveReceived(receiveMessageStorageModel).GetAwaiter().GetResult();
            // 进入接收消息处理队列
            MessageReceiveQueueProvider.Enqueue(receiveMessageStorageModel, message.Items, Descriptor);
        }
    }
}