using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.MemoryQueue
{
    public class MemoryEventSubscriber : IEventSubscriber
    {
        public MemoryEventSubscriber(IMessageSender messageSender, ILogger<MemoryEventSubscriber> logger)
        {
            Logger = logger;
            MessageSender = messageSender as MemoryMessageSender;
        }

        private MemoryMessageSender MessageSender { get; }
        private ILogger<MemoryEventSubscriber> Logger { get; }

        public Task Subscribe(IMessageListener listener, CancellationToken token)
        {
            MessageSender.OnMessageReceived += async (sender, e) =>
            {
                if (token.IsCancellationRequested)
                    return;
                MessageTransferModel message = e.MessageTransferModel;

                if (message is null)
                {
                    Logger.LogError($"[EventBus-Memory: {listener.Descriptor.EventHandlerName}] deserialize message from rabbit error.");
                    return;
                }

                if (message.EventName != listener.Descriptor.EventName)
                    return;

                Logger.LogDebug(
                    $"[EventBus-Memory: {listener.Descriptor.EventHandlerName}] received msg: {message.ToJson()}.");

                while (true)
                {
                    try
                    {
                        // 处理消息
                        await listener.OnReceive(message, token).ConfigureAwait(false);
                        return;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex,
                            $"[EventBus-Memory: {listener.Descriptor.EventHandlerName}]  received msg execute OnReceive error: {message.ToJson()}.");
                    }

                    Task.Delay(100, token).ConfigureAwait(false).GetAwaiter().GetResult();
                }
            };

            return Task.CompletedTask;
        }
    }
}