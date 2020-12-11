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

            InternalMemoryQueue.StartAsync(HostedStopToken.StopCancellationToken);
        }

        private IMessageListener MessageListener { get; }
        private ILogger<MemoryEventSubscriber> Logger { get; }
        private IHostedStopToken HostedStopToken { get; }

        private ConcurrentDictionary<string, ConcurrentBag<EventHandlerDescriptor>> Listeners { get; } =
            new ConcurrentDictionary<string, ConcurrentBag<EventHandlerDescriptor>>();


        public Task Subscribe(EventHandlerDescriptor eventHandlerDescriptor, CancellationToken token)
        {
            AddListener(eventHandlerDescriptor);

            InternalMemoryQueue.OnReceived += msg =>
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
                        InternalMemoryQueue.Send(msg);
                });
            };
            return Task.CompletedTask;
        }

        public void AddListener(EventHandlerDescriptor descriptor)
        {
            var list = Listeners.GetOrAdd(descriptor.EventName, new ConcurrentBag<EventHandlerDescriptor>());
            list.Add(descriptor);
        }
    }
}