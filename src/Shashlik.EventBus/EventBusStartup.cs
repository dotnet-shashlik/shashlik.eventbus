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
        private IMessageCunsumerRegistry MessageCunsumerRegistry { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IMessageStorage MessageStorage { get; }
        private EventBusOptions EventBusOptions { get; }
        private IMessageReceiveQueueProvider MessageReceiveQueueProvider { get; }
        private IPublishedMessageRetryProvider PublishedMessageRetryProvider { get; }
        private IReceivedMessageRetryProvider ReceivedMessageRetryProvider { get; }

        public EventBusStartup(IMessageStorageInitializer messageStorageInitializer,
            IEventHandlerFindProvider eventHandlerFindProvider, IMessageCunsumerRegistry messageCunsumerRegistry,
            IMessageSerializer messageSerializer, IMessageStorage messageStorage,
            IMessageReceiveQueueProvider messageReceiveQueueProvider, IOptions<EventBusOptions> eventBusOptions,
            IPublishedMessageRetryProvider publishedMessageRetryProvider,
            IReceivedMessageRetryProvider receivedMessageRetryProvider)
        {
            MessageStorageInitializer = messageStorageInitializer;
            EventHandlerFindProvider = eventHandlerFindProvider;
            MessageCunsumerRegistry = messageCunsumerRegistry;
            MessageSerializer = messageSerializer;
            MessageStorage = messageStorage;
            MessageReceiveQueueProvider = messageReceiveQueueProvider;
            PublishedMessageRetryProvider = publishedMessageRetryProvider;
            ReceivedMessageRetryProvider = receivedMessageRetryProvider;
            EventBusOptions = eventBusOptions.Value;
        }

        public async Task Build(CancellationToken cancellationToken)
        {
            // 先执行存储设施初始化
            await MessageStorageInitializer.Initialize(cancellationToken);

            // 注册监听器
            var descriptors = EventHandlerFindProvider.LoadAll();
            var environment = EventBusOptions.Environment;

            foreach (var eventHandlerDescriptor in descriptors)
            {
                MessageCunsumerRegistry.Subscribe(new DefaultMessageListener(eventHandlerDescriptor, environment,
                    MessageSerializer, MessageStorage, MessageReceiveQueueProvider));
            }

            // 启动重试器
            await PublishedMessageRetryProvider.DoRetry(cancellationToken);
            // 启动重试器
            await ReceivedMessageRetryProvider.DoRetry(cancellationToken);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Build(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            //TODO: stop token
            return Task.CompletedTask;
        }
    }
}