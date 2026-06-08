using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.RelationDbStorage;

namespace Shashlik.EventBus.MySql;

public class MySqlFreeSqlFactory : IFreeSqlFactory, IDisposable
{
    public MySqlFreeSqlFactory(ILoggerFactory loggerFactory, IConnectionString connectionString,
        IOptionsMonitor<EventBusMySqlOptions> options)
    {
        LoggerFactory = loggerFactory;
        ConnectionString = connectionString;
        Options = options;
        _freeSql = new Lazy<IFreeSql>(Build, isThreadSafe: true);
    }

    private ILoggerFactory LoggerFactory { get; }
    private IConnectionString ConnectionString { get; }
    private IOptionsMonitor<EventBusMySqlOptions> Options { get; }

    // 实例级缓存。原来的 static volatile IFreeSql? 让多个 factory 实例共享同一 FreeSql,
    // 一旦 Options 切到新连接串也不再生效,Dispose 还会把别人的 FreeSql 一起释放。
    // 改为 Lazy<T>(true) 拿到按实例缓存 + thread-safe 初始化的语义。
    private readonly Lazy<IFreeSql> _freeSql;

    public IFreeSql Instance() => _freeSql.Value;

    private IFreeSql Build()
    {
        var logger = LoggerFactory.CreateLogger("EventBusMysql");
        var freeSql = new FreeSql.FreeSqlBuilder()
            .UseConnectionString(FreeSql.DataType.MySql, ConnectionString.ConnectionString)
            .UseMonitorCommand(cmd => logger.LogDebug($"Sql：{cmd.CommandText}"))
            .Build();

        // 配置表名和 schema
        freeSql.Aop.ConfigEntity += (_, e) =>
        {
            if (e.EntityType == typeof(RelationDbMessageStoragePublishedModel))
                e.ModifyResult.Name = $"{Options.CurrentValue.PublishedTableName}";
            else if (e.EntityType == typeof(RelationDbMessageStorageReceivedModel))
                e.ModifyResult.Name = $"{Options.CurrentValue.ReceivedTableName}";
        };
        return freeSql;
    }

    public void Dispose()
    {
        if (_freeSql.IsValueCreated)
            _freeSql.Value.Dispose();
    }
}
