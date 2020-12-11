using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Shashlik.Utils.Extensions;

// ReSharper disable SimplifyLinqExpressionUseAll

namespace Shashlik.EventBus.Kafka
{
    public class KafkaEventSubscriber : IEventSubscriber
    {
        public KafkaEventSubscriber(IKafkaConnection connection, IMessageSerializer messageSerializer,
            ILogger<KafkaEventSubscriber> logger, IMessageListener messageListener)
        {
            Connection = connection;
            MessageSerializer = messageSerializer;
            Logger = logger;
            MessageListener = messageListener;
        }

        private IKafkaConnection Connection { get; }
        private IMessageSerializer MessageSerializer { get; }
        private ILogger<KafkaEventSubscriber> Logger { get; }
        private IMessageListener MessageListener { get; }

        public async Task Subscribe(EventHandlerDescriptor eventHandlerDescriptor, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            if (cancellationToken.IsCancellationRequested)
                return;

            var consumer = Connection.CreateConsumer(eventHandlerDescriptor.EventHandlerName);
            consumer.Subscribe(eventHandlerDescriptor.EventName);

            var eventName = eventHandlerDescriptor.EventName;
            var eventHandlerName = eventHandlerDescriptor.EventHandlerName;

            _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ConsumeResult<string, byte[]> consumerResult;
                    try
                    {
                        consumerResult = consumer.Consume(cancellationToken);
                        if (consumerResult.IsPartitionEOF || consumerResult.Message.Value.IsNullOrEmpty())
                        {
                            // ReSharper disable once MethodSupportsCancellation
                            await Task.Delay(5).ConfigureAwait(false);
                            continue;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // ReSharper disable once MethodSupportsCancellation
                        await Task.Delay(5).ConfigureAwait(false);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex,
                            $"[EventBus-Kafka] consume message occur error, event: {eventName}, handler: {eventHandlerName}.");
                        // ReSharper disable once MethodSupportsCancellation
                        await Task.Delay(5).ConfigureAwait(false);
                        continue;
                    }

                    _ = Task.Run(async () =>
                    {
                        MessageTransferModel message;
                        try
                        {
                            message = MessageSerializer.Deserialize<MessageTransferModel>(consumerResult.Message.Value);
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(e, "[EventBus-Kafka] deserialize message from kafka error.");
                            return;
                        }

                        if (message == null)
                        {
                            Logger.LogError("[EventBus-Kafka] deserialize message from kafka error.");
                            return;
                        }

                        if (message.EventName != eventName)
                        {
                            Logger.LogError(
                                $"[EventBus-Kafka] received invalid event name \"{message.EventName}\", expect \"{eventName}\".");
                            return;
                        }

                        Logger.LogDebug(
                            $"[EventBus-Kafka] received msg: {message.ToJson()}.");

                        // 执行消息监听处理
                        var res = await MessageListener.OnReceive(eventHandlerName, message, cancellationToken).ConfigureAwait(false);
                        // 存储偏移,确认消费, see: https://docs.confluent.io/current/clients/dotnet.html
                        if (res == MessageReceiveResult.Success)
                            // 只有监听处理成功才提交偏移量,否则不处理即可
                            consumer.StoreOffset(consumerResult);
                    }, cancellationToken);

                    // ReSharper disable once MethodSupportsCancellation
                    await Task.Delay(5).ConfigureAwait(false);
                }
            }, cancellationToken);
        }
    }
}