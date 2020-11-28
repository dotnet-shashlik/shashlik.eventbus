using System.Threading.Tasks;

namespace Shashlik.EventBus.MemoryQueue
{
    /// <summary>
    /// 消息发送处理类
    /// </summary>
    public class MemoryMessageSender : IMessageSender
    {
        internal event OnMessageReceivedHandler OnMessageReceived;

        public Task Send(MessageTransferModel message)
        {
            var _ = Task.Run(() =>
            {
                OnMessageReceived?.Invoke(this, new OnMessageTransferEventArgs
                {
                    MessageTransferModel = message
                });
            });

            return Task.CompletedTask;
        }
    }
}