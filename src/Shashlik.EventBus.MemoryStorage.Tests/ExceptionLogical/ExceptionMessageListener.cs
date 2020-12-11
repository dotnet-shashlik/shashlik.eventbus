using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Shashlik.EventBus.MemoryStorage.Tests.ExceptionLogical
{
    public class ExceptionMessageListener : IMessageListener
    {
        public static ConcurrentBag<string> MsgIds { get; } = new ConcurrentBag<string>();

        public async Task<MessageReceiveResult> OnReceive(string eventHandlerName, MessageTransferModel messageTransferModel,
            CancellationToken cancellationToken)
        {
            MsgIds.Add(messageTransferModel.MsgId);
            if (MsgIds.Count > 3)
                return MessageReceiveResult.Success;
            return await Task.FromResult(MessageReceiveResult.Failed);
        }
    }
}