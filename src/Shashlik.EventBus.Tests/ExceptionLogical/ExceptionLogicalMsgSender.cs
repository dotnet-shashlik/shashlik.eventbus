using System.Threading.Tasks;

namespace Shashlik.EventBus.Tests.ExceptionLogical
{
    public class ExceptionLogicalMsgSender : IMessageSender
    {
        public Task Send(MessageTransferModel message)
        {
            throw new System.NotImplementedException();
        }
    }
}