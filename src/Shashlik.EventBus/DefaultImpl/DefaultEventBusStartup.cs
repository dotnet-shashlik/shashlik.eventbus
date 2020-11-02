using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.DefaultImpl
{
    public class DefaultEventBusStartup : IHostedService
    {
        private IMessageStorageInitializer MessageStorageInitializer { get; }
        private IEventHandlerFindProvider EventHandlerFindProvider { get; }
        private IMessageCunsumerRegistry MessageCunsumerRegistry { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IMessageStorage MessageStorage { get; }
        private EventBusOptions EventBusOptions { get; }
        private IMessageReceiveQueueProvider MessageReceiveQueueProvider { get; }

        public DefaultEventBusStartup(IMessageStorageInitializer messageStorageInitializer,
            IEventHandlerFindProvider eventHandlerFindProvider, IMessageCunsumerRegistry messageCunsumerRegistry,
            IMessageSerializer messageSerializer, IMessageStorage messageStorage,
            IMessageReceiveQueueProvider messageReceiveQueueProvider, IOptions<EventBusOptions> eventBusOptions)
        {
            MessageStorageInitializer = messageStorageInitializer;
            EventHandlerFindProvider = eventHandlerFindProvider;
            MessageCunsumerRegistry = messageCunsumerRegistry;
            MessageSerializer = messageSerializer;
            MessageStorage = messageStorage;
            MessageReceiveQueueProvider = messageReceiveQueueProvider;
            EventBusOptions = eventBusOptions.Value;
        }

        public void Build()
        {
            // 先执行存储设施初始化
            MessageStorageInitializer.Initialize();

            // 注册监听器
            var descriptors = EventHandlerFindProvider.LoadAll();
            var environment = EventBusOptions.Environment;

            foreach (var eventHandlerDescriptor in descriptors)
            {
                MessageCunsumerRegistry.Subscribe(new DefaultMessageListener(eventHandlerDescriptor, environment,
                    MessageSerializer, MessageStorage, MessageReceiveQueueProvider));
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Build();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            //throw new System.NotImplementedException();
            return Task.CompletedTask;
        }
    }
}