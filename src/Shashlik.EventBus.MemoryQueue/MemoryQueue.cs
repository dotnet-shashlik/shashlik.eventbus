using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.MemoryQueue
{
    public class MemoryQueue : IHostedService
    {
        public MemoryQueue(IMessageListener messageListener, ILogger<MemoryQueue> logger, IHostedStopToken hostedStopToken)
        {
            MessageListener = messageListener;
            Logger = logger;
            HostedStopToken = hostedStopToken;
        }

        private ConcurrentQueue<MessageTransferModel> Queue { get; } = new ConcurrentQueue<MessageTransferModel>();

        private ConcurrentDictionary<string, ConcurrentBag<EventHandlerDescriptor>> Listeners { get; } =
            new ConcurrentDictionary<string, ConcurrentBag<EventHandlerDescriptor>>();

        private IMessageListener MessageListener { get; }
        private ILogger<MemoryQueue> Logger { get; }
        private IHostedStopToken HostedStopToken { get; }

        public void Startup()
        {
            _ = Task.Run(() =>
            {
                while (!HostedStopToken.StopCancellationToken.IsCancellationRequested)
                {
                    if (Queue.TryDequeue(out var msg))
                    {
                        var listeners = Listeners.GetOrDefault(msg.EventName);
                        Parallel.ForEach(listeners, async descriptor =>
                        {
                            if (HostedStopToken.StopCancellationToken.IsCancellationRequested)
                                return;

                            Logger.LogDebug($"[EventBus-Memory: {descriptor.EventHandlerName}] received msg: {msg.ToJson()}.");

                            // 处理消息
                            var res = await MessageListener.OnReceive(descriptor.EventHandlerName, msg, HostedStopToken.StopCancellationToken)
                                .ConfigureAwait(false);
                            if (res != MessageReceiveResult.Success)
                                Send(msg);
                        });
                    }

                    // ReSharper disable once MethodSupportsCancellation
                    Task.Delay(10).ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }, HostedStopToken.StopCancellationToken);
        }

        public void Send(MessageTransferModel messageTransferModel)
        {
            Queue.Enqueue(messageTransferModel);
        }

        public void AddListener(EventHandlerDescriptor descriptor)
        {
            var list = Listeners.GetOrAdd(descriptor.EventName, new ConcurrentBag<EventHandlerDescriptor>());
            list.Add(descriptor);
        }

        public Task StartAsync(CancellationToken _)
        {
            Startup();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken _)
        {
            return Task.CompletedTask;
        }
    }
}