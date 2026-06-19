using System;
using FreeRedis;
using Microsoft.Extensions.DependencyInjection;

namespace Shashlik.EventBus.Redis;

public class EventBusRedisWorkerIdOptions
{
    /// <summary>
    /// 应用名称
    /// </summary>
    public string AppName { get; set; } = "DEFAULT";

    /// <summary>
    /// redis client配置
    /// </summary>
    public Func<IServiceProvider, RedisClient?>? RedisClientFactory { get; set; } =
        s => s.GetService<RedisClient>();
}