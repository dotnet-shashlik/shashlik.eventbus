using System;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.DefaultImpl;

/// <summary>
/// 主id生成器, 基于自定义雪花算法, 从环境变量WORKER_ID(0~1023)中读取, 否则使用随机值.
/// <para>
/// SnowflakeIdGenerator 由 DI 容器以单例形式注册, 其内部持有的 <see cref="Snowflake"/> 实例随本对象生命周期唯一.
/// </para>
/// </summary>
public class SnowflakeIdGenerator : IIdGenerator
{
    private readonly Snowflake _snowflake;

    public SnowflakeIdGenerator(IServiceProvider serviceProvider, IOptions<EventBusOptions> options)
    {
        _snowflake = new Snowflake(options.Value.WorkerIdFactory(serviceProvider));
    }

    public long NextId()
    {
        return _snowflake.NextId();
    }
}