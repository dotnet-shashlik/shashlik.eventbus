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
    /// 消息堆积最大数量,默认0
    /// </summary>
    public int MaxLength { get; set; }

    /// <summary>
    /// 消息堆积最大数量动态配置器,优先级比更高
    /// </summary>
    public Func<MessageTransferModel, int>? MaxLengthFactory { get; set; }
}