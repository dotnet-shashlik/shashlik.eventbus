using System;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Shashlik.EventBus.StackExchangeRedis;

/// <summary>
/// 基于 StackExchange.Redis 的事件总线统一配置:同时适用于 MQ 和 WorkerId 两类功能.
/// </summary>
public class EventBusStackExchangeRedisOptions
{
    /// <summary>
    /// redis IConnectionMultiplexer 工厂, 默认从 DI 容器获取 <see cref="IConnectionMultiplexer"/> 单例.
    /// </summary>
    public Func<IServiceProvider, IConnectionMultiplexer?>? ConnectionMultiplexerFactory { get; set; } =
        s => s.GetService<IConnectionMultiplexer>();

    /// <summary>
    /// 应用名称 (WorkerId 分配使用).
    /// </summary>
    public string AppName { get; set; } = "DEFAULT";

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
