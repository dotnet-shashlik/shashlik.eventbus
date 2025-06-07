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
    }

    private ILoggerFactory LoggerFactory { get; }
    private IConnectionString ConnectionString { get; }
    private IOptionsMonitor<EventBusSqlServerOptions> Options { get; }
    private static volatile IFreeSql? _freeSql;
    private static readonly object Locker = new();

    public IFreeSql Instance()
    {
        if (_freeSql is not null)
            return _freeSql;
        lock (Locker)
        {
            if (_freeSql is not null)
                return _freeSql;
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

            _freeSql = freeSql;
            return _freeSql;
        }
    }

    public void Dispose()
    {
        _freeSql?.Dispose();
    }
}