using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.MemoryQueue
{
    internal static class InternalMemoryQueue
    {
        private static ConcurrentQueue<MessageTransferModel> Queue { get; } = new ConcurrentQueue<MessageTransferModel>();

        private static ConcurrentDictionary<string, ConcurrentBag<IMessageListener>> Listeners { get; } =
            new ConcurrentDictionary<string, ConcurrentBag<IMessageListener>>();

        public static void Start(ILogger logger, CancellationToken cancellationToken)
        {
            _ = Task.Run(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (Queue.TryDequeue(out var msg))
                    {
                        var listeners = Listeners.GetOrDefault(msg.EventName);
                        Parallel.ForEach(listeners, async listener =>
                        {
                            if (cancellationToken.IsCancellationRequested)
                                return;

                            if (msg.EventName != listener.Descriptor.EventName)
                                return;

                            logger.LogDebug($"[EventBus-Memory: {listener.Descriptor.EventHandlerName}] received msg: {msg.ToJson()}.");

                            try
                            {
                                // 处理消息
                                await listener.OnReceive(msg, cancellationToken).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                logger.LogDebug(ex,
                                    $"[EventBus-Memory: {listener.Descriptor.EventHandlerName}]OnReceive occur error: {msg.ToJson()}.");
                                // 一般是存储异常了，再重新加入队列
                                Send(msg);
                            }
                        });
                    }

                    Task.Delay(10, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }, cancellationToken);
        }

        public static void Send(MessageTransferModel messageTransferModel)
        {
            Queue.Enqueue(messageTransferModel);
        }

        public static void AddListener(IMessageListener listener)
        {
            var list = Listeners.GetOrAdd(listener.Descriptor.EventName, new ConcurrentBag<IMessageListener>());
            list.Add(listener);
        }
    }
}