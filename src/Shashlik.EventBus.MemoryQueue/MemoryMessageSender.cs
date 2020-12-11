using System.Threading.Tasks;

namespace Shashlik.EventBus.MemoryQueue
{
    /// <summary>
    /// 消息发送处理类
    /// </summary>
    public class MemoryMessageSender : IMessageSender
    {
        public MemoryMessageSender(MemoryQueue memoryQueue)
        {
            MemoryQueue = memoryQueue;
        }

        private MemoryQueue MemoryQueue { get; }
        
        public Task Send(MessageTransferModel message)
        {
            MemoryQueue.Send(message);
            return Task.CompletedTask;
        }
    }
}