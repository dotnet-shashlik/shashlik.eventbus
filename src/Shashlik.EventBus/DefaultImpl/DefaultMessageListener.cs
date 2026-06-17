using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.Utils;

// ReSharper disable MethodSupportsCancellation

namespace Shashlik.EventBus.DefaultImpl
{
    public class DefaultMessageListener : IMessageListener
    {
        public DefaultMessageListener(
            IMessageSerializer messageSerializer,
            IMessageStorage messageStorage,
            IEventHandlerFindProvider eventHandlerFindProvider,
            ILogger<DefaultMessageListener> logger,
            IOptions<EventBusOptions> options,
            IReceivedHandler receivedHandler, IIdGenerator idGenerator)
        {
            MessageSerializer = messageSerializer;
            MessageStorage = messageStorage;
            EventHandlerFindProvider = eventHandlerFindProvider;
            Logger = logger;
            Options = options;
            ReceivedHandler = receivedHandler;
            IdGenerator = idGenerator;
        }

        private IMessageSerializer MessageSerializer { get; }
        private IMessageStorage MessageStorage { get; }
        private IEventHandlerFindProvider EventHandlerFindProvider { get; }
        private ILogger<DefaultMessageListener> Logger { get; }
        private IOptions<EventBusOptions> Options { get; }
        private IReceivedHandler ReceivedHandler { get; }
        private IIdGenerator IdGenerator { get; }

        public async Task<MessageReceiveResult> OnReceiveAsync(string eventHandlerName, MessageTransferModel message,
            CancellationToken cancellationToken)
        {
            try
            {
                var descriptor = EventHandlerFindProvider.GetByName(eventHandlerName);
                if (descriptor is null)
                {
                    Logger.LogError($"[EventBus] can not found event handler: {eventHandlerName}");
                    return MessageReceiveResult.Failed;
                }

                var now = DateTimeOffset.Now;
                message.Items ??= new Dictionary<string, string>();
                var receiveMessageStorageModel = new MessageStorageModel
                {
                    Id = IdGenerator.NextId(),
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

                await MessageStorage
                    .SaveReceivedAsync(receiveMessageStorageModel, cancellationToken)
                    .ConfigureAwait(false);

                // 非延迟事件直接进入执行队列
                if (!message.DelayAt.HasValue || message.DelayAt.Value <= DateTimeOffset.Now)
                    Start(receiveMessageStorageModel, message.Items, descriptor, cancellationToken);
                // 延迟事件进入延迟执行队列
                else if (message.DelayAt.HasValue && (message.DelayAt.Value - DateTimeOffset.Now).TotalSeconds <=
                         (Options.Value.StartRetryAfter * 2))
                {
                    void Action() => Start(receiveMessageStorageModel, message.Items, descriptor, cancellationToken);
                    //TODO: 内存溢出问题
                    TimerHelper.SetTimeout(
                        Action,
                        message.DelayAt.Value,
                        cancellationToken);
                }

                return MessageReceiveResult.Success;
            }
            catch (Exception ex)
            {
                // 详细记录错误,便于排查"OnReceiveAsync 返回 Failed"的根因
                Logger.LogError(ex,
                    $"[EventBus] message listener handle message occur error, handlerName={eventHandlerName}, msgId={message?.MsgId}");
                return MessageReceiveResult.Failed;
            }
        }

        private void Start(
            MessageStorageModel messageStorageModel,
            IDictionary<string, string> items,
            EventHandlerDescriptor descriptor,
            CancellationToken cancellationToken)
        {
            _ = Task.Run(async () =>
            {
                // 执行失败的次数
                var failCount = 1;
                while (!cancellationToken.IsCancellationRequested)
                {
                    var handleResult = await ReceivedHandler
                        .HandleAsync(messageStorageModel, items, descriptor, cancellationToken)
                        .ConfigureAwait(false);

                    if (!handleResult.Success)
                        failCount++;
                    else
                        return;

                    if (failCount > 5)
                    {
                        // 5次都失败了,交给重试器去执行了,这里就不管了
                        return;
                    }

                    // ReSharper disable once MethodSupportsCancellation
                    await Task.Delay(10).ConfigureAwait(false);
                }
            }, cancellationToken);
        }
    }
}