using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shashlik.EventBus.Utils;
// ReSharper disable AsyncVoidLambda

namespace Shashlik.EventBus.MemoryQueue
{
    public class MemoryEventSubscriber : IEventSubscriber, IDisposable
    {
        // 整个 AppDomain 共享一份 Listeners 注册表(订阅关系跨多 host/进程内多容器共用),
        // 共享一份后台消费循环。早期实现把 Listeners/MessageListener 等实例字段
        // 通过静态闭包捕获,导致后续 DI 解析的 MemoryEventSubscriber 实例注册的事件
        // 永远不会被触发(闭包里捕获的是首实例的 Listeners,后续 Listeners.Clear() 又会清掉它)。
        // 这里改成:Listener 字典改成 static,处理函数每次从静态字典里查最新的订阅者;
        // 每个实例只在静态字典里维护自己的订阅。Dispose 时按"event name"清掉自己注册的
        // 那些(通过记录 instance->descriptors 映射来精确定位)。
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<MemoryEventSubscriber, ConcurrentBag<EventHandlerDescriptor>>>
            Listeners =
                new ConcurrentDictionary<string, ConcurrentDictionary<MemoryEventSubscriber, ConcurrentBag<EventHandlerDescriptor>>>();

        private static int _started;
        private static Action<MessageTransferModel>? _onReceivedHandler;
        private static readonly object StartLock = new();

        // 仅在 EnsureStarted 第一次调用时用到,后续 Dispose 也不应该把它停掉
        // (整个 AppDomain 共享消费循环),用静态 CancellationTokenSource 以便进程内统一停。
        private static readonly CancellationTokenSource StaticCts = new();

        public MemoryEventSubscriber(ILogger<MemoryEventSubscriber> logger, IMessageListener messageListener,
            IHostedStopToken hostedStopToken)
        {
            Logger = logger;
            MessageListener = messageListener;
            HostedStopToken = hostedStopToken;
            // 整进程只启动一次后台消费循环
            EnsureStarted();
        }

        private IMessageListener MessageListener { get; }
        private ILogger<MemoryEventSubscriber> Logger { get; }
        private IHostedStopToken HostedStopToken { get; }

        public Task SubscribeAsync(EventHandlerDescriptor descriptor, CancellationToken token)
        {
            var perInstance = Listeners.GetOrAdd(
                descriptor.EventName,
                _ => new ConcurrentDictionary<MemoryEventSubscriber, ConcurrentBag<EventHandlerDescriptor>>());
            perInstance.GetOrAdd(this, _ => new ConcurrentBag<EventHandlerDescriptor>()).Add(descriptor);
            return Task.CompletedTask;
        }

        private void EnsureStarted()
        {
            if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
                return;

            lock (StartLock)
            {
                if (_onReceivedHandler is not null)
                    return;

                _onReceivedHandler = msg =>
                {
                    if (!Listeners.TryGetValue(msg.EventName, out var perInstance)) return;
                    // 拍快照后遍历,订阅变化不影响本次派发
                    foreach (var kvp in perInstance.ToArray())
                    {
                        if (kvp.Value.IsNullOrEmpty()) continue;
                        var subscriber = kvp.Key;
                        if (subscriber.HostedStopToken.StopCancellationToken.IsCancellationRequested)
                            continue;

                        foreach (var descriptor in kvp.Value)
                        {
                            try
                            {
                                var res = subscriber.MessageListener
                                    .OnReceiveAsync(descriptor.EventHandlerName, msg,
                                        subscriber.HostedStopToken.StopCancellationToken)
                                    .GetAwaiter().GetResult();
                                if (res != MessageReceiveResult.Success)
                                    InternalMemoryQueue.Send(msg);
                            }
                            catch (Exception ex)
                            {
                                subscriber.Logger.LogError(ex,
                                    $"[EventBus-Memory] handler \"{descriptor.EventHandlerName}\" threw, re-enqueueing");
                                InternalMemoryQueue.Send(msg);
                            }
                        }
                    }
                };
                InternalMemoryQueue.OnReceived += _onReceivedHandler;
                InternalMemoryQueue.Start(StaticCts.Token);
            }
        }

        public void Dispose()
        {
            // 摘除本实例注册的所有订阅
            foreach (var perInstance in Listeners.Values)
            {
                perInstance.TryRemove(this, out _);
            }
            // 测试场景下,多 host 共用同一内存队列;旧 host 残留的消息(已被新 host
            // 的 MemoryEventSubscriber 重新入队的也算)在 Dispose 时一并清掉,避免新 host
            // 看到旧环境/旧 handler 名而报"can not found event handler"。
            InternalMemoryQueue.Clear();
        }
    }
}
