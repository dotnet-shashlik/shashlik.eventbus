using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Shashlik.EventBus;

namespace CommonTestLogical.MsgWithoutLosing
{
    public class MsgWithoutLosingListener : IMessageListener
    {
        public MsgWithoutLosingListener(IMessageSerializer messageSerializer)
        {
            MessageSerializer = messageSerializer;
        }

        public static ConcurrentBag<string> MsgIds { get; } = new ConcurrentBag<string>();
        private IMessageSerializer MessageSerializer { get; }


        public async Task<MessageReceiveResult> OnReceiveAsync(string eventHandlerName, MessageTransferModel messageTransferModel,
            CancellationToken cancellationToken)
        {
            MsgIds.Add(messageTransferModel.MsgId);
            if (MsgIds.Count > 3)
                return MessageReceiveResult.Success;
            return await Task.FromResult(MessageReceiveResult.Failed);
        }
    }
}