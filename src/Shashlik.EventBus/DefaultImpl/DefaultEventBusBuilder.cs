using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.DefaultImpl
{
    public class DefaultEventBusBuilder : IEventBusBuilder
    {
        public DefaultEventBusBuilder(IServiceCollection serviceCollection)
        {
            ServiceCollection = serviceCollection;
        }

        public IServiceCollection ServiceCollection { get; }

        public IServiceCollection Build()
        {
            GlobalServiceCollection.ServiceCollection = ServiceCollection;

            using var serviceProvider = ServiceCollection.BuildServiceProvider();

            // 先执行存储设施初始化
            var messageStorageInitializer = serviceProvider.GetRequiredService<IMessageStorageInitializer>();
            messageStorageInitializer.Initialize();

            // 注册监听器
            var descriptors = serviceProvider.GetRequiredService<IEventHandlerFindProvider>().LoadAll();
            var messageCunsumerRegistry = serviceProvider.GetRequiredService<IMessageCunsumerRegistry>();
            var messageSerializer = serviceProvider.GetRequiredService<IMessageSerializer>();
            var messageStorage = serviceProvider.GetRequiredService<IMessageStorage>();

            var messageReceiveQueueProvider = serviceProvider.GetRequiredService<IMessageReceiveQueueProvider>();
            var environment = serviceProvider.GetRequiredService<IOptions<EventBusOptions>>().Value.Environment;

            foreach (var eventHandlerDescriptor in descriptors)
            {
                messageCunsumerRegistry.Subscribe(new DefaultMessageListener(eventHandlerDescriptor, environment,
                    messageSerializer, messageStorage, messageReceiveQueueProvider));
            }

            return ServiceCollection;
        }
    }
}