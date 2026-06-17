using System.Threading.Tasks;
using MessagePipe;

namespace Shashlik.EventBus.MemoryQueue
{
    public class MemoryMessageSender : IMessageSender
    {
        private readonly IPublisher<string, MessageTransferModel> _publisher;

        public MemoryMessageSender(IPublisher<string, MessageTransferModel> publisher)
        {
            _publisher = publisher;
        }

        public Task SendAsync(MessageTransferModel message)
        {
            _publisher.Publish(message.EventName, message);
            return Task.CompletedTask;
        }
    }
}
