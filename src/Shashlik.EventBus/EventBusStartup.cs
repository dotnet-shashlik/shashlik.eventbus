using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Shashlik.Utils.Helpers;

namespace Shashlik.EventBus
{
    public class EventBusStartup : IHostedService, IHostedStopToken, IDisposable
    {
        private IMessageStorageInitializer MessageStorageInitializer { get; }
        private IEventHandlerFindProvider EventHandlerFindProvider { get; }
        private IEventSubscriber EventSubscriber { get; }
        private IPublishedMessageRetryProvider PublishedMessageRetryProvider { get; }
        private IReceivedMessageRetryProvider ReceivedMessageRetryProvider { get; }
        private IExpiredMessageProvider ExpiredMessageProvider { get; }
        private CancellationTokenSource StopCancellationTokenSource { get; }
        public CancellationToken StopCancellationToken => StopCancellationTokenSource.Token;

        public EventBusStartup(
            IMessageStorageInitializer messageStorageInitializer,
            IEventHandlerFindProvider eventHandlerFindProvider,
            IEventSubscriber eventSubscriber,
            IPublishedMessageRetryProvider publishedMessageRetryProvider,
            IReceivedMessageRetryProvider receivedMessageRetryProvider,
            IExpiredMessageProvider expiredMessageProvider)
        {
            MessageStorageInitializer = messageStorageInitializer;
            EventHandlerFindProvider = eventHandlerFindProvider;
            EventSubscriber = eventSubscriber;
            PublishedMessageRetryProvider = publishedMessageRetryProvider;
            ReceivedMessageRetryProvider = receivedMessageRetryProvider;
            ExpiredMessageProvider = expiredMessageProvider;
            StopCancellationTokenSource = new CancellationTokenSource();
        }

        public async Task Build()
        {
            // 先执行存储设施初始化
            await MessageStorageInitializer.Initialize(StopCancellationToken).ConfigureAwait(false);

            // 加载所有的事件处理类
            var descriptors = EventHandlerFindProvider.FindAll();

            // 注册事件订阅
            foreach (var eventHandlerDescriptor in descriptors)
                await EventSubscriber.Subscribe(eventHandlerDescriptor, StopCancellationToken).ConfigureAwait(false);

            // 启动发送消息重试器
            await PublishedMessageRetryProvider.Startup(StopCancellationToken).ConfigureAwait(false);
            // 启动接收消息重试器
            await ReceivedMessageRetryProvider.Startup(StopCancellationToken).ConfigureAwait(false);
            // 启动过期消息删除
            await ExpiredMessageProvider.DoDelete(StopCancellationToken);

            // 每分钟执行一次垃圾回收，考虑到大量的异步逻辑可能带来的对象释放问题
            TimerHelper.SetInterval(() =>
            {
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }, TimeSpan.FromSeconds(3));
        }

        public async Task StartAsync(CancellationToken _)
        {
            await Build().ConfigureAwait(false);
        }

        public Task StopAsync(CancellationToken _)
        {
            StopCancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            try
            {
                StopCancellationTokenSource.Cancel();
                StopCancellationTokenSource.Dispose();
            }
            catch
            {
                // ignored
            }
        }
    }
}