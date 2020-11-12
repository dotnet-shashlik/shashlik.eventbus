using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Shashlik.Utils.Helpers;

namespace Shashlik.EventBus.DefaultImpl
{
    /// <summary>
    /// 已接收延迟事件处理器
    /// </summary>
    public class DefaultReceivedDelayEventProvider : IReceivedDelayEventProvider
    {
        public DefaultReceivedDelayEventProvider(
            IMessageStorage messageStorage,
            IOptionsMonitor<EventBusOptions> options,
            IMessageReceiveQueueProvider messageReceiveQueueProvider)
        {
            MessageStorage = messageStorage;
            Options = options;
            MessageReceiveQueueProvider = messageReceiveQueueProvider;
        }

        private IMessageStorage MessageStorage { get; }
        private IOptionsMonitor<EventBusOptions> Options { get; }
        private IMessageReceiveQueueProvider MessageReceiveQueueProvider { get; }

        public void Enqueue(MessageStorageModel message, IDictionary<string, string> items,
            EventHandlerDescriptor descriptor, CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                if (!message.DelayAt.HasValue)
                    return;
                if (message.DelayAt.Value <= DateTimeOffset.Now)
                    await Invoke(message, items, descriptor, cancellationToken);
                else
                    TimerHelper.SetTimeout(
                        async () => await Invoke(message, items, descriptor, cancellationToken),
                        message.DelayAt!.Value,
                        cancellationToken);
            }, cancellationToken);
        }

        public async Task Invoke(MessageStorageModel message, IDictionary<string, string> items,
            EventHandlerDescriptor descriptor, CancellationToken cancellationToken)
        {
            if (await MessageStorage.TryLockReceived(
                message.MsgId,
                true,
                DateTimeOffset.Now.AddSeconds(Options.CurrentValue.RetryIntervalSeconds).GetLongDate(),
                cancellationToken))
                MessageReceiveQueueProvider.Enqueue(message, items, descriptor, cancellationToken);
        }
    }
}