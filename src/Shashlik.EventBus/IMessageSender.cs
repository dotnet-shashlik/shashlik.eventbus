using System.Threading.Tasks;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 消息发送
    /// </summary>
    public interface IMessageSender
    {
        Task Send(MessageTransferModel message);
    }
}