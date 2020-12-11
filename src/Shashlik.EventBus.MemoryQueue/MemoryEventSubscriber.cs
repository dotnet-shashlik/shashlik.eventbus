using System.Threading;
using System.Threading.Tasks;

namespace Shashlik.EventBus.MemoryQueue
{
    public class MemoryEventSubscriber : IEventSubscriber
    {
        public MemoryEventSubscriber(MemoryQueue memoryQueue)
        {
            MemoryQueue = memoryQueue;
        }

        private MemoryQueue MemoryQueue { get; }

        public Task Subscribe(EventHandlerDescriptor eventHandlerDescriptor, CancellationToken token)
        {
            MemoryQueue.AddListener(eventHandlerDescriptor);
            return Task.CompletedTask;
        }
    }
}