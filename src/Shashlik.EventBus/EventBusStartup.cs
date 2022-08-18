using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Shashlik.EventBus.DefaultImpl;

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
            await MessageStorageInitializer.InitializeAsync(HostedStopToken.StopCancellationToken).ConfigureAwait(false);

            // 加载所有的事件处理类
            var descriptors = EventHandlerFindProvider.FindAll();

            // 注册事件订阅
            foreach (var eventHandlerDescriptor in descriptors)
                await EventSubscriber.SubscribeAsync(eventHandlerDescriptor, HostedStopToken.StopCancellationToken).ConfigureAwait(false);

            // 启动发送消息重试器
            await PublishedMessageRetryProvider.StartupAsync(HostedStopToken.StopCancellationToken).ConfigureAwait(false);
            // 启动接收消息重试器
            await ReceivedMessageRetryProvider.StartupAsync(HostedStopToken.StopCancellationToken).ConfigureAwait(false);
            // 启动过期消息删除
            await ExpiredMessageProvider.DoDeleteAsync(HostedStopToken.StopCancellationToken);
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