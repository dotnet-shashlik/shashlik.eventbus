using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.DefaultImpl;

namespace Shashlik.EventBus
{
    public class EventBusStartup : IHostedService
    {
        private IMessageStorageInitializer MessageStorageInitializer { get; }
        private IEventHandlerFindProvider EventHandlerFindProvider { get; }
        private IEventSubscriber EventSubscriber { get; }
        private IPublishedMessageRetryProvider PublishedMessageRetryProvider { get; }
        private IReceivedMessageRetryProvider ReceivedMessageRetryProvider { get; }
        private IMessageListenerFactory MessageListenerFactory { get; }
        private CancellationTokenSource StopCancellationTokenSource { get; }

        public EventBusStartup(
            IMessageStorageInitializer messageStorageInitializer,
            IEventHandlerFindProvider eventHandlerFindProvider,
            IEventSubscriber eventSubscriber,
            IPublishedMessageRetryProvider publishedMessageRetryProvider,
            IReceivedMessageRetryProvider receivedMessageRetryProvider,
            IMessageListenerFactory messageListenerFactory)
        {
            MessageStorageInitializer = messageStorageInitializer;
            EventHandlerFindProvider = eventHandlerFindProvider;
            EventSubscriber = eventSubscriber;
            PublishedMessageRetryProvider = publishedMessageRetryProvider;
            ReceivedMessageRetryProvider = receivedMessageRetryProvider;
            MessageListenerFactory = messageListenerFactory;
            StopCancellationTokenSource = new CancellationTokenSource();
        }

        public async Task Build()
        {
            // 先执行存储设施初始化
            await MessageStorageInitializer.Initialize(StopCancellationTokenSource.Token);

            // 加载所有的事件处理类
            var descriptors = EventHandlerFindProvider.LoadAll();

            // 注册事件订阅
            foreach (var eventHandlerDescriptor in descriptors)
            {
                var listener = MessageListenerFactory
                    .CreateMessageListener(eventHandlerDescriptor);
                EventSubscriber.Subscribe(listener, StopCancellationTokenSource.Token);
            }

            // 启动重试器
            await PublishedMessageRetryProvider.DoRetry(StopCancellationTokenSource.Token);
            // 启动重试器
            await ReceivedMessageRetryProvider.DoRetry(StopCancellationTokenSource.Token);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Build();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopCancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }
    }
}