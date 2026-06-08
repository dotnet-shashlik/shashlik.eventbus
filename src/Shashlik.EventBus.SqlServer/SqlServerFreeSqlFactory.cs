using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.RelationDbStorage;

namespace Shashlik.EventBus.SqlServer;

public class SqlServerFreeSqlFactory : IFreeSqlFactory, IDisposable
{
    public SqlServerFreeSqlFactory(ILoggerFactory loggerFactory, IConnectionString connectionString,
        IOptionsMonitor<EventBusSqlServerOptions> options)
    {
        LoggerFactory = loggerFactory;
        ConnectionString = connectionString;
        Options = options;
        _freeSql = new Lazy<IFreeSql>(Build, isThreadSafe: true);
    }

    private ILoggerFactory LoggerFactory { get; }
    private IConnectionString ConnectionString { get; }
    private IOptionsMonitor<EventBusSqlServerOptions> Options { get; }
    private readonly Lazy<IFreeSql> _freeSql;

    public IFreeSql Instance() => _freeSql.Value;

    private IFreeSql Build()
    {
        var logger = LoggerFactory.CreateLogger("EventBusSqlServer");
        var freeSql = new FreeSql.FreeSqlBuilder()
            .UseConnectionString(FreeSql.DataType.SqlServer, ConnectionString.ConnectionString)
            .UseMonitorCommand(cmd => logger.LogDebug($"Sql：{cmd.CommandText}"))
            .Build();
        // 配置表名和 schema
        freeSql.Aop.ConfigEntity += (_, e) =>
        {
            if (e.EntityType == typeof(RelationDbMessageStoragePublishedModel))
                e.ModifyResult.Name = $"{Options.CurrentValue.Schema}.{Options.CurrentValue.PublishedTableName}";
            else if (e.EntityType == typeof(RelationDbMessageStorageReceivedModel))
                e.ModifyResult.Name = $"{Options.CurrentValue.Schema}.{Options.CurrentValue.ReceivedTableName}";
        };
        return freeSql;
    }

    public void Dispose()
    {
        if (_freeSql.IsValueCreated)
            _freeSql.Value.Dispose();
    }
}
