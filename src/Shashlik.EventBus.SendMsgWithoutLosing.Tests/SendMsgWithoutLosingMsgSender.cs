using System.Threading.Tasks;

namespace Shashlik.EventBus.SendMsgWithoutLosing.Tests
{
    public class SendMsgWithoutLosingMsgSender : IMessageSender
    {
        public Task SendAsync(MessageTransferModel message)
        {
            throw new System.NotImplementedException();
        }
    }
}