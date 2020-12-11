using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.DefaultImpl
{
    public class DefaultMessageListener : IMessageListener
    {
        public DefaultMessageListener(
            IMessageSerializer messageSerializer,
            IMessageStorage messageStorage,
            IMessageReceiveQueueProvider messageReceiveQueueProvider,
            IReceivedDelayEventProvider receivedDelayEventProvider, IEventHandlerFindProvider eventHandlerFindProvider,
            ILogger<DefaultMessageListener> logger, IOptions<EventBusOptions> options)
        {
            MessageSerializer = messageSerializer;
            MessageStorage = messageStorage;
            MessageReceiveQueueProvider = messageReceiveQueueProvider;
            ReceivedDelayEventProvider = receivedDelayEventProvider;
            EventHandlerFindProvider = eventHandlerFindProvider;
            Logger = logger;
            Options = options;
        }

        private IMessageSerializer MessageSerializer { get; }
        private IMessageStorage MessageStorage { get; }
        private IMessageReceiveQueueProvider MessageReceiveQueueProvider { get; }
        private IReceivedDelayEventProvider ReceivedDelayEventProvider { get; }
        private IEventHandlerFindProvider EventHandlerFindProvider { get; }
        private ILogger<DefaultMessageListener> Logger { get; }
        private IOptions<EventBusOptions> Options { get; }

        public async Task<MessageReceiveResult> OnReceive(string eventHandlerName, MessageTransferModel message, CancellationToken cancellationToken)
        {
            try
            {
                var descriptor = EventHandlerFindProvider.GetByName(eventHandlerName);
                var now = DateTime.Now;
                message.Items ??= new Dictionary<string, string>();
                var receiveMessageStorageModel = new MessageStorageModel
                {
                    MsgId = message.MsgId,
                    Environment = message.Environment ?? Options.Value.Environment,
                    CreateTime = now,
                    ExpireTime = null,
                    EventHandlerName = eventHandlerName,
                    EventName = message.EventName,
                    RetryCount = 0,
                    Status = MessageStatus.Scheduled,
                    IsLocking = false,
                    LockEnd = null,
                    EventItems = MessageSerializer.Serialize(message.Items),
                    EventBody = message.MsgBody,
                    DelayAt = message.DelayAt
                };

                var existsModel = await MessageStorage.FindReceivedByMsgId(message.MsgId, descriptor, cancellationToken).ConfigureAwait(false);
                // 保存接收到的消息
                if (existsModel is null)
                    await MessageStorage.SaveReceived(receiveMessageStorageModel, cancellationToken).ConfigureAwait(false);
                else
                    receiveMessageStorageModel.Id = existsModel.Id;

                // 非延迟事件直接进入执行队列
                if (!message.DelayAt.HasValue)
                    // 进入接收消息处理队列
                    MessageReceiveQueueProvider.Enqueue(receiveMessageStorageModel, message.Items, descriptor,
                        cancellationToken);
                // 延迟事件进入延迟执行队列
                else
                    ReceivedDelayEventProvider.Enqueue(receiveMessageStorageModel, message.Items, descriptor,
                        cancellationToken);

                return MessageReceiveResult.Success;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"[EventBus] message listening occur error.");
                return MessageReceiveResult.Failed;
            }
        }
    }
}