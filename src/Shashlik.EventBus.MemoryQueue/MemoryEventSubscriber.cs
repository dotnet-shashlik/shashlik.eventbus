using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.MemoryQueue
{
    public class MemoryEventSubscriber : IEventSubscriber
    {
        public MemoryEventSubscriber(ILogger<MemoryEventSubscriber> logger, IMessageListener messageListener,
            IHostedStopToken hostedStopToken)
        {
            Logger = logger;
            MessageListener = messageListener;
            HostedStopToken = hostedStopToken;
            Start();
        }

        private IMessageListener MessageListener { get; }
        private ILogger<MemoryEventSubscriber> Logger { get; }
        private IHostedStopToken HostedStopToken { get; }

        private ConcurrentDictionary<string, ConcurrentBag<EventHandlerDescriptor>> Listeners { get; } =
            new ConcurrentDictionary<string, ConcurrentBag<EventHandlerDescriptor>>();


        public Task SubscribeAsync(EventHandlerDescriptor descriptor, CancellationToken token)
        {
            var list = Listeners.GetOrAdd(descriptor.EventName, new ConcurrentBag<EventHandlerDescriptor>());
            list.Add(descriptor);
            return Task.CompletedTask;
        }

        private void Start()
        {
            InternalMemoryQueue.OnReceived += msg =>
            {
                var listeners = Listeners.GetOrDefault(msg.EventName);
                if (listeners.IsNullOrEmpty())
                {
                    Logger.LogWarning($"[EventBus-Memory] received msg of {msg.EventName}, but not found associated event handlers.");
                    return;
                }

                Parallel.ForEach(listeners!, async descriptor =>
                {
                    if (HostedStopToken.StopCancellationToken.IsCancellationRequested)
                        return;

                    Logger.LogDebug($"[EventBus-Memory: {descriptor.EventHandlerName}] received msg: {msg}-{msg.MsgBody}.");

                    // 处理消息
                    var res = await MessageListener.OnReceiveAsync(descriptor.EventHandlerName, msg, HostedStopToken.StopCancellationToken)
                        .ConfigureAwait(false);
                    if (res != MessageReceiveResult.Success)
                        InternalMemoryQueue.Send(msg);
                });
            };

            InternalMemoryQueue.Start(HostedStopToken.StopCancellationToken);
        }
    }
}