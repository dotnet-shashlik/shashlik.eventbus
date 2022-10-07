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
                        OnReceived?.Invoke(msg);

                    // ReSharper disable once MethodSupportsCancellation
                    await Task.Delay(10).ConfigureAwait(false);
                }
            }, stopCancellationToken);
        }
    }
}