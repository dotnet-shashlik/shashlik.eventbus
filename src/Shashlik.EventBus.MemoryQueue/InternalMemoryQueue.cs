using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Shashlik.EventBus.MemoryQueue
{
    internal static class InternalMemoryQueue
    {
        private static ConcurrentQueue<MessageTransferModel> Queue { get; } = new();
        public static event Action<MessageTransferModel>? OnReceived;

        public static void Send(MessageTransferModel messageTransferModel)
        {
            Queue.Enqueue(messageTransferModel);
        }

        public static void Start(CancellationToken stopCancellationToken)
        {
            _ = Task.Run(async () =>
            {
                while (!stopCancellationToken.IsCancellationRequested)
                {
                    if (Queue.TryDequeue(out var msg))
                    {
                        try
                        {
                            OnReceived?.Invoke(msg);
                        }
                        catch
                        {
                            // 单个 handler 抛错不应拖垮消费循环,MemoryEventSubscriber 内部已 try/catch
                        }
                    }

                    // ReSharper disable once MethodSupportsCancellation
                    await Task.Delay(10).ConfigureAwait(false);
                }
            }, stopCancellationToken);
        }

        /// <summary>
        /// 排空队列中残留的消息。在 WebApplicationFactory.Dispose 时调用,
        /// 避免上一个 host 的测试残留消息被新 host 的消费循环处理,导致"找不到 handler"。
        /// </summary>
        public static void Clear()
        {
            while (Queue.TryDequeue(out _))
            {
            }
        }
    }
}
