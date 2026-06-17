using System;
using System.Threading;
using System.Threading.Tasks;
using MessagePipe;
using Microsoft.Extensions.Logging;

namespace Shashlik.EventBus.MemoryQueue
{
    public class MemoryEventSubscriber : IEventSubscriber, IDisposable
    {
        private readonly ISubscriber<string, MessageTransferModel> _subscriber;
        private readonly IPublisher<string, MessageTransferModel> _publisher;
        private readonly ILogger<MemoryEventSubscriber> _logger;
        private readonly IMessageListener _messageListener;
        private readonly IHostedStopToken _hostedStopToken;
        private readonly DisposableBagBuilder _disposableBag = DisposableBag.CreateBuilder();

        public MemoryEventSubscriber(
            ISubscriber<string, MessageTransferModel> subscriber,
            IPublisher<string, MessageTransferModel> publisher,
            ILogger<MemoryEventSubscriber> logger,
            IMessageListener messageListener,
            IHostedStopToken hostedStopToken)
        {
            _subscriber = subscriber;
            _publisher = publisher;
            _logger = logger;
            _messageListener = messageListener;
            _hostedStopToken = hostedStopToken;
        }

        public Task SubscribeAsync(EventHandlerDescriptor descriptor, CancellationToken token)
        {
            _subscriber.Subscribe(descriptor.EventName, msg =>
            {
                if (_hostedStopToken.StopCancellationToken.IsCancellationRequested)
                    return;

                try
                {
                    var res = _messageListener
                        .OnReceiveAsync(descriptor.EventHandlerName, msg,
                            _hostedStopToken.StopCancellationToken)
                        .GetAwaiter().GetResult();
                    if (res != MessageReceiveResult.Success)
                        _publisher.Publish(msg.EventName, msg);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        $"[EventBus-Memory] handler \"{descriptor.EventHandlerName}\" threw, re-enqueueing");
                    _publisher.Publish(msg.EventName, msg);
                }
            }).AddTo(_disposableBag);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _disposableBag.Build().Dispose();
        }
    }
}
