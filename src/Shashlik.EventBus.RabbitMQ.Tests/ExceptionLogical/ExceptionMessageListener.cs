using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Shashlik.EventBus.RabbitMQ.Tests.ExceptionLogical
{
    public class ExceptionMessageListener : IMessageListener
    {
        public ExceptionMessageListener(IMessageSerializer messageSerializer)
        {
            MessageSerializer = messageSerializer;
        }

        public static ConcurrentBag<string> MsgIds { get; } = new ConcurrentBag<string>();
        private IMessageSerializer MessageSerializer { get; }


        public async Task<MessageReceiveResult> OnReceiveAsync(string eventHandlerName, MessageTransferModel messageTransferModel,
            CancellationToken cancellationToken)
        {
            var @event = MessageSerializer.Deserialize<ExceptionLogicalTestEvent>(messageTransferModel.MsgBody);
            if (@event.Id != ExceptionLogicalTestEvent._Id)
                return MessageReceiveResult.Success;

            MsgIds.Add(messageTransferModel.MsgId);
            if (MsgIds.Count > 3)
                return MessageReceiveResult.Success;
            return await Task.FromResult(MessageReceiveResult.Failed);
        }
    }
}