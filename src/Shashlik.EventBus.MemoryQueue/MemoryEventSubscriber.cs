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
        private static int _started;
        private static Action<MessageTransferModel>? _onReceivedHandler;
        private static readonly object StartLock = new();

        public MemoryEventSubscriber(ILogger<MemoryEventSubscriber> logger, IMessageListener messageListener,
            IHostedStopToken hostedStopToken)
        {
            Logger = logger;
            MessageListener = messageListener;
            HostedStopToken = hostedStopToken;
            Listeners = new ConcurrentDictionary<string, ConcurrentBag<EventHandlerDescriptor>>();
            // 整个 AppDomain 内只启动一次 InternalMemoryQueue,只订阅一次 OnReceived,
            // 避免每次 DI 解析 MemoryEventSubscriber 时都追加新的 handler 造成泄漏。
            EnsureStarted();
        }

        private IMessageListener MessageListener { get; }
        private ILogger<MemoryEventSubscriber> Logger { get; }
        private IHostedStopToken HostedStopToken { get; }
        private ConcurrentDictionary<string, ConcurrentBag<EventHandlerDescriptor>> Listeners { get; }

        public Task SubscribeAsync(EventHandlerDescriptor descriptor, CancellationToken token)
        {
            var list = Listeners.GetOrAdd(descriptor.EventName, new ConcurrentBag<EventHandlerDescriptor>());
            list.Add(descriptor);
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
                    var listeners = Listeners.GetOrDefault(msg.EventName);
                    if (listeners.IsNullOrEmpty())
                    {
                        Logger.LogWarning(
                            $"[EventBus-Memory] received msg of {msg.EventName}, but not found associated event handlers");
                        return;
                    }

                    foreach (var descriptor in listeners!)
                    {
                        if (HostedStopToken.StopCancellationToken.IsCancellationRequested)
                            return;

                        Logger.LogDebug(
                            $"[EventBus-Memory: {descriptor.EventHandlerName}] received msg: {msg}-{msg.MsgBody}");

                        // 同步等待 listener 完成,失败时重新入队(保持原有重试语义)。
                        // 这里不能再用 Parallel.ForEach + async lambda 模式,会丢异常。
                        try
                        {
                            var res = MessageListener
                                .OnReceiveAsync(descriptor.EventHandlerName, msg,
                                    HostedStopToken.StopCancellationToken)
                                .GetAwaiter().GetResult();
                            if (res != MessageReceiveResult.Success)
                                InternalMemoryQueue.Send(msg);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex,
                                $"[EventBus-Memory] handler \"{descriptor.EventHandlerName}\" threw, re-enqueueing");
                            InternalMemoryQueue.Send(msg);
                        }
                    }
                };
                InternalMemoryQueue.OnReceived += _onReceivedHandler;
                InternalMemoryQueue.Start(HostedStopToken.StopCancellationToken);
            }
        }

        public void Dispose()
        {
            // 静态事件无法按实例解绑,只能保留引用待 GC。下次 DI 解析新实例时
            // EnsureStarted 会短路。这里把 Listeners 清空,防止 process 内还持有旧 handler。
            Listeners.Clear();
        }
    }
}