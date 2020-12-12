using System.Threading.Tasks;

namespace Shashlik.EventBus.Tests.SendMsgWithoutLosing
{
    public class SendMsgWithoutLosingMsgSender : IMessageSender
    {
        public Task SendAsync(MessageTransferModel message)
        {
            throw new System.NotImplementedException();
        }
    }
}