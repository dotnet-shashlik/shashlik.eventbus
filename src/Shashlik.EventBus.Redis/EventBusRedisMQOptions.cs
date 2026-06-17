using System;
using FreeRedis;
using Microsoft.Extensions.DependencyInjection;

namespace Shashlik.EventBus.Redis;

public class EventBusRedisMQOptions
{
    /// <summary>
    /// redis client配置
    /// </summary>
    public Func<IServiceProvider, RedisClient?>? RedisClientFactory { get; set; } =
        s => s.GetService<RedisClient>();

    /// <summary>
    /// 消息堆积最大数量,默认10000,当堆积数量超过这个值时,新的消息将被丢弃,直到堆积数量降到这个值以下,这个可以防止消息堆积过多导致内存溢出
    /// </summary>
    public int MaxLength { get; set; } = 10000;

    /// <summary>
    /// 每个 handler 的并发消费者数量,默认 4。
    /// 大于 1 时,框架为同一个 handler 创建多个消费者 task,
    /// Redis Streams 会在同一个 consumer group 内自动将消息分发给不同的消费者。
    /// </summary>
    public int ConsumerPoolSize { get; set; } = 4;

    /// <summary>
    /// 消息堆积最大数量动态配置器,优先级比<see cref="MaxLength"/>更高,这个可以根据不同的事件配置不同的堆积数量
    /// </summary>
    public Func<MessageTransferModel, int>? MaxLengthFactory { get; set; }
}