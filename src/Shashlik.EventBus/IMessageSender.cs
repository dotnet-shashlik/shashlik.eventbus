using System.Threading.Tasks;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 消息发送
    /// </summary>
    public interface IMessageSender
    {
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message">消息传输模型</param>
        /// <returns></returns>
        Task Send(MessageTransferModel message);
    }
}