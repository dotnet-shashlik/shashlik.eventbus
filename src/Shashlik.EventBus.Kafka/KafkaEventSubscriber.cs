using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.Kafka
{
    public class KafkaEventSubscriber : IEventSubscriber, IDisposable
    {
        public KafkaEventSubscriber(IKafkaConnection connection, IMessageSerializer messageSerializer,
            ILogger<KafkaEventSubscriber> logger, IMessageListener messageListener,
            IOptions<EventBusKafkaOptions> options)
        {
            Connection = connection;
            MessageSerializer = messageSerializer;
            Logger = logger;
            MessageListener = messageListener;
            Options = options;
        }

        private IKafkaConnection Connection { get; }
        private IMessageSerializer MessageSerializer { get; }
        private ILogger<KafkaEventSubscriber> Logger { get; }
        private IMessageListener MessageListener { get; }
        private IOptions<EventBusKafkaOptions> Options { get; }

        private readonly ConcurrentBag<IConsumer<string, byte[]>> _consumerList = [];

        // 新增：记录每个消费者各个分区的退避状态
        // Key: $"{Topic}-{Partition.Value}"
        private readonly ConcurrentDictionary<string, PartitionBackoffState> _partitionStates = new();

        // 退避时间配置
        private const int NormalTimeoutMs = 100;
        private const int InitialBackoffMs = 1000;
        private const int MaxBackoffMs = 30000; // 最大退避 30 秒

        private class PartitionBackoffState
        {
            public bool IsPaused { get; set; }
            public int CurrentBackoffMs { get; set; } = InitialBackoffMs;
        }

        public async Task SubscribeAsync(EventHandlerDescriptor eventHandlerDescriptor,
            CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            if (cancellationToken.IsCancellationRequested)
                return;

            for (var i = 0; i < Options.Value.ConsumerPoolSize; i++)
            {
                var consumer = Connection.CreateConsumer(eventHandlerDescriptor.EventHandlerName,
                    eventHandlerDescriptor.EventName);
                _consumerList.Add(consumer);
                consumer.Subscribe(eventHandlerDescriptor.EventName);
                
                var eventName = eventHandlerDescriptor.EventName;
                var eventHandlerName = eventHandlerDescriptor.EventHandlerName;

                _ = Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            await Consume(consumer, eventName, eventHandlerName, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        catch (AccessViolationException)
                        {
                            // ignore
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(e, $"[EventBus-Kafka] consume message occur error");
                        }

                        // 外层循环的微小延迟，防止极端情况下的 CPU 空转
                        await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                    }
                }, cancellationToken);
            }
        }

        private async Task Consume(IConsumer<string, byte[]> consumer, string eventName, string eventHandlerName,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            // 1. 计算当前应该使用的 Consume Timeout
            // 如果有任何分区处于 Pause 退避状态，使用最大的退避时间作为 Timeout，以维持心跳并降低 CPU 占用
            int timeoutMs = NormalTimeoutMs;
            foreach (var state in _partitionStates.Values)
            {
                if (state.IsPaused && state.CurrentBackoffMs > timeoutMs)
                {
                    timeoutMs = state.CurrentBackoffMs;
                }
            }

            ConsumeResult<string, byte[]> consumerResult;
            try
            {
                // 2. 使用 TimeSpan 调用 Consume。
                // 在 Pause 期间，这里会阻塞 timeoutMs 毫秒，期间 librdkafka 后台线程会持续发送心跳，不会触发 Rebalance！
                consumerResult = consumer.Consume(TimeSpan.FromMilliseconds(timeoutMs));

                if (consumerResult is null || consumerResult.IsPartitionEOF ||
                    consumerResult.Message.Value.IsNullOrEmpty())
                    return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    $"[EventBus-Kafka] consume message occur error, event: {eventName}, handler: {eventHandlerName}");
                return;
            }

            MessageTransferModel? message;
            try
            {
                message = MessageSerializer.Deserialize<MessageTransferModel>(consumerResult.Message.Value);
            }
            catch (Exception e)
            {
                Logger.LogError(e,
                    $"[EventBus-Kafka] deserialize message from kafka error: {Encoding.UTF8.GetString(consumerResult.Message.Value)}");
                return; // 反序列化失败属于毒消息，直接跳过，不重试
            }

            if (message is null)
            {
                Logger.LogError("[EventBus-Kafka] deserialize message from kafka error");
                return;
            }

            if (message.EventName != eventName)
            {
                Logger.LogError(
                    $"[EventBus-Kafka] received invalid event name \"{message.EventName}\", expect \"{eventName}\"");
                return;
            }

            Logger.LogDebug($"[EventBus-Kafka] received msg: {message}");

            // 执行消息监听处理
            var res = await MessageListener.OnReceiveAsync(eventHandlerName, message, cancellationToken)
                .ConfigureAwait(false);

            var partitionKey = $"{consumerResult.Topic}-{consumerResult.Partition.Value}";

            if (res == MessageReceiveResult.Success)
            {
                consumer.Commit(consumerResult);

                if (_partitionStates.TryRemove(partitionKey, out var recoveredState) && recoveredState.IsPaused)
                {
                    consumer.Resume([consumerResult.TopicPartition]);
                    Logger.LogInformation("[EventBus-Kafka] 数据库已恢复，分区 {Partition} 已恢复消费",
                        consumerResult.Partition.Value);
                }
            }
            else
            {
                if (!_partitionStates.TryGetValue(partitionKey, out var state))
                {
                    state = new PartitionBackoffState();
                    _partitionStates[partitionKey] = state;
                }

                if (!state.IsPaused)
                {
                    // 第一次失败：回退 offset 并暂停分区
                    consumer.Seek(consumerResult.TopicPartitionOffset);
                    consumer.Pause([consumerResult.TopicPartition]);
                    state.IsPaused = true;
                    state.CurrentBackoffMs = InitialBackoffMs;
                    Logger.LogWarning("[EventBus-Kafka] Message processing failed, partition {Partition} enters backoff mode, will retry after {Backoff}ms",
                        consumerResult.Partition.Value, state.CurrentBackoffMs);
                }
                else
                {
                    // 仍然失败：指数退避（封顶 30 秒）
                    state.CurrentBackoffMs = Math.Min(state.CurrentBackoffMs * 2, MaxBackoffMs);
                    Logger.LogWarning("[EventBus-Kafka] Message processing still failed, partition {Partition} continues backoff, will retry after {Backoff}ms",
                        consumerResult.Partition.Value, state.CurrentBackoffMs);
                }
            }
        }

        public void Dispose()
        {
            foreach (var consumer in _consumerList)
            {
                try
                {
                    consumer.Close();
                    consumer.Dispose();
                }
                catch
                {
                    //ignore
                }
            }
        }
    }
}