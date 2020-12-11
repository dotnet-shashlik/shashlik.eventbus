using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.MemoryQueue
{
    internal static class InternalMemoryQueue
    {
        private static ConcurrentQueue<MessageTransferModel> Queue { get; } = new ConcurrentQueue<MessageTransferModel>();
        public static event Action<MessageTransferModel>? OnReceived;

        public static void Send(MessageTransferModel messageTransferModel)
        {
            Queue.Enqueue(messageTransferModel);
        }

        public static void StartAsync(CancellationToken stopCancellationToken)
        {
            _ = Task.Run(() =>
            {
                while (!stopCancellationToken.IsCancellationRequested)
                {
                    if (Queue.TryDequeue(out var msg))
                        OnReceived?.Invoke(msg);

                    // ReSharper disable once MethodSupportsCancellation
                    Task.Delay(10).ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }, stopCancellationToken);
        }
    }
}