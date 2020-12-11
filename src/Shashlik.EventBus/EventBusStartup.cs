using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Shashlik.EventBus.DefaultImpl;
using Shashlik.Utils.Helpers;

namespace Shashlik.EventBus
{
    public class EventBusStartup : IHostedService
    {
        public EventBusStartup(
            IMessageStorageInitializer messageStorageInitializer,
            IEventHandlerFindProvider eventHandlerFindProvider,
            IEventSubscriber eventSubscriber,
            IPublishedMessageRetryProvider publishedMessageRetryProvider,
            IReceivedMessageRetryProvider receivedMessageRetryProvider,
            IExpiredMessageProvider expiredMessageProvider, IHostedStopToken hostedStopToken)
        {
            MessageStorageInitializer = messageStorageInitializer;
            EventHandlerFindProvider = eventHandlerFindProvider;
            EventSubscriber = eventSubscriber;
            PublishedMessageRetryProvider = publishedMessageRetryProvider;
            ReceivedMessageRetryProvider = receivedMessageRetryProvider;
            ExpiredMessageProvider = expiredMessageProvider;
            HostedStopToken = hostedStopToken;
        }

        private IMessageStorageInitializer MessageStorageInitializer { get; }
        private IEventHandlerFindProvider EventHandlerFindProvider { get; }
        private IEventSubscriber EventSubscriber { get; }
        private IPublishedMessageRetryProvider PublishedMessageRetryProvider { get; }
        private IReceivedMessageRetryProvider ReceivedMessageRetryProvider { get; }
        private IExpiredMessageProvider ExpiredMessageProvider { get; }
        private IHostedStopToken HostedStopToken { get; }

        public async Task Build()
        {
            // 先执行存储设施初始化
            await MessageStorageInitializer.Initialize(HostedStopToken.StopCancellationToken).ConfigureAwait(false);

            // 加载所有的事件处理类
            var descriptors = EventHandlerFindProvider.FindAll();

            // 注册事件订阅
            foreach (var eventHandlerDescriptor in descriptors)
                await EventSubscriber.Subscribe(eventHandlerDescriptor, HostedStopToken.StopCancellationToken).ConfigureAwait(false);

            // 启动发送消息重试器
            await PublishedMessageRetryProvider.Startup(HostedStopToken.StopCancellationToken).ConfigureAwait(false);
            // 启动接收消息重试器
            await ReceivedMessageRetryProvider.Startup(HostedStopToken.StopCancellationToken).ConfigureAwait(false);
            // 启动过期消息删除
            await ExpiredMessageProvider.DoDelete(HostedStopToken.StopCancellationToken);

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
            (HostedStopToken as InternalHostedStopToken)!.Cancel();
            return Task.CompletedTask;
        }
    }
}