using System.Threading;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.DefaultImpl
{
    /// <summary>
    /// 消息监听器创建工厂接口
    /// </summary>
    public class DefaultMessageListenerFactory : IMessageListenerFactory
    {
        public DefaultMessageListenerFactory(IOptionsMonitor<EventBusOptions> options,
            IMessageSerializer messageSerializer, IMessageStorage messageStorage,
            IMessageReceiveQueueProvider messageReceiveQueueProvider)
        {
            Options = options;
            MessageSerializer = messageSerializer;
            MessageStorage = messageStorage;
            MessageReceiveQueueProvider = messageReceiveQueueProvider;
        }

        private IOptionsMonitor<EventBusOptions> Options { get; }
        private IMessageSerializer MessageSerializer { get; }
        private IMessageStorage MessageStorage { get; }
        private IMessageReceiveQueueProvider MessageReceiveQueueProvider { get; }

        public IMessageListener CreateMessageListener(EventHandlerDescriptor descriptor)
        {
            return new DefaultMessageListener(descriptor, Options.CurrentValue.Environment, MessageSerializer,
                MessageStorage, MessageReceiveQueueProvider);
        }
    }
}