using System.Threading.Tasks;

namespace Shashlik.EventBus.MemoryQueue
{
    /// <summary>
    /// 消息发送处理类
    /// </summary>
    public class MemoryMessageSender : IMessageSender
    {
        public Task Send(MessageTransferModel message)
        {
            InternalMemoryQueue.Send(message);
            return Task.CompletedTask;
        }
    }
}