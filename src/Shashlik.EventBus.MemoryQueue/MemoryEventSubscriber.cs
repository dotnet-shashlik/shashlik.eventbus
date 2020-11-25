using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.MemoryQueue
{
    public class MemoryEventSubscriber : IEventSubscriber
    {
        public MemoryEventSubscriber(IMessageSender messageSender, Logger<MemoryEventSubscriber> logger)
        {
            Logger = logger;
            MessageSender = messageSender as MemoryMessageSender;
        }

        private MemoryMessageSender MessageSender { get; }
        private Logger<MemoryEventSubscriber> Logger { get; }

        public void Subscribe(IMessageListener listener, CancellationToken token)
        {
            MessageSender.OnMessageReceived += (sender, e) =>
            {
                MessageTransferModel message = e.MessageTransferModel;

                if (message == null)
                {
                    Logger.LogError("[EventBus-Memory] deserialize message from rabbit error.");
                    return;
                }

                if (message.EventName != listener.Descriptor.EventName)
                {
                    Logger.LogError(
                        $"[EventBus-Memory] received invalid event name \"{message.EventName}\", expect \"{listener.Descriptor.EventName}\".");
                    return;
                }

                Logger.LogDebug(
                    $"[EventBus-Memory] received msg: {message.ToJson()}.");

                while (true)
                {
                    try
                    {
                        // 处理消息
                        listener.OnReceive(message, token);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex,
                            $"[EventBus-Memory] received msg execute OnReceive error: {message.ToJson()}.");
                    }

                    Thread.Sleep(20);
                }
            };
        }
    }
}