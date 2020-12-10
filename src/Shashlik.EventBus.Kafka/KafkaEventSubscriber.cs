using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.Kafka
{
    public class KafkaEventSubscriber : IEventSubscriber
    {
        public KafkaEventSubscriber(IKafkaConnection connection, IMessageSerializer messageSerializer,
            ILogger<KafkaEventSubscriber> logger)
        {
            Connection = connection;
            MessageSerializer = messageSerializer;
            Logger = logger;
        }

        private IKafkaConnection Connection { get; }
        private IMessageSerializer MessageSerializer { get; }
        private ILogger<KafkaEventSubscriber> Logger { get; }

        public Task Subscribe(IMessageListener listener, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.CompletedTask;
            IConsumer<string, byte[]> consumer = Connection.CreateCunsumer(listener.Descriptor.EventHandlerName);
            consumer.Subscribe(listener.Descriptor.EventName);
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
                            await Task.Delay(5, cancellationToken).ConfigureAwait(false);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex,
                            $"[EventBus-Kafka]Consume message occur error, event: {listener.Descriptor.EventName}, handler: {listener.Descriptor.EventHandlerName}.");
                        await Task.Delay(5, cancellationToken).ConfigureAwait(false);
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

                        if (message.EventName != listener.Descriptor.EventName)
                        {
                            Logger.LogError(
                                $"[EventBus-Kafka] received invalid event name \"{message.EventName}\", expect \"{listener.Descriptor.EventName}\".");
                            return;
                        }

                        Logger.LogDebug(
                            $"[EventBus-Kafka] received msg: {message.ToJson()}.");

                        try
                        {
                            // 处理消息
                            await listener.OnReceive(message, cancellationToken).ConfigureAwait(false);
                            // 存储偏移,提交消息, see: https://docs.confluent.io/current/clients/dotnet.html
                            consumer.StoreOffset(consumerResult);
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(e,
                                $"[EventBus-Kafka] received msg execute OnReceive error: {message.ToJson()}.");
                        }
                    }, cancellationToken);

                    await Task.Delay(5, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }
    }
}