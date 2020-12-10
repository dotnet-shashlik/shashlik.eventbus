using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Shashlik.EventBus
{
    public class EventBusStartup : IHostedService, IHostedStopToken, IDisposable
    {
        private IMessageStorageInitializer MessageStorageInitializer { get; }
        private IEventHandlerFindProvider EventHandlerFindProvider { get; }
        private IEventSubscriber EventSubscriber { get; }
        private IPublishedMessageRetryProvider PublishedMessageRetryProvider { get; }
        private IReceivedMessageRetryProvider ReceivedMessageRetryProvider { get; }
        private IMessageListenerFactory MessageListenerFactory { get; }
        private IExpiredMessageProvider ExpiredMessageProvider { get; }
        private CancellationTokenSource StopCancellationTokenSource { get; }
        public CancellationToken StopCancellationToken => StopCancellationTokenSource.Token;

        public EventBusStartup(
            IMessageStorageInitializer messageStorageInitializer,
            IEventHandlerFindProvider eventHandlerFindProvider,
            IEventSubscriber eventSubscriber,
            IPublishedMessageRetryProvider publishedMessageRetryProvider,
            IReceivedMessageRetryProvider receivedMessageRetryProvider,
            IMessageListenerFactory messageListenerFactory,
            IExpiredMessageProvider expiredMessageProvider)
        {
            MessageStorageInitializer = messageStorageInitializer;
            EventHandlerFindProvider = eventHandlerFindProvider;
            EventSubscriber = eventSubscriber;
            PublishedMessageRetryProvider = publishedMessageRetryProvider;
            ReceivedMessageRetryProvider = receivedMessageRetryProvider;
            MessageListenerFactory = messageListenerFactory;
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
            {
                var listener = MessageListenerFactory.CreateMessageListener(eventHandlerDescriptor);
                await EventSubscriber.Subscribe(listener, StopCancellationToken).ConfigureAwait(false);
            }

            // 启动重试器
            await PublishedMessageRetryProvider.Startup(StopCancellationToken).ConfigureAwait(false);
            // 启动重试器
            await ReceivedMessageRetryProvider.Startup(StopCancellationToken).ConfigureAwait(false);
            // 启动过期消息删除
            await ExpiredMessageProvider.DoDelete(StopCancellationToken);
        }

        public async Task StartAsync(CancellationToken _)
        {
            await Build().ConfigureAwait(false);
        }

        public Task StopAsync(CancellationToken _)
        {
            StopCancellationTokenSource.Cancel();
            GC.Collect();
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