using FreeSql;
using FreeSql.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.RelationDbStorage;

/// <summary>
/// <see cref="IFreeSqlFactory"/> 的默认实现:根据 <see cref="EventBusRelationDbOptions.DataType"/>
/// 选用对应的 FreeSql Provider,所有方言(MySQL/PG/SqlServer/Sqlite/...)走同一份代码。
/// </summary>
internal class DefaultFreeSqlFactory : IFreeSqlFactory
{
    public DefaultFreeSqlFactory(
        IOptions<EventBusRelationDbOptions> options,
        ILoggerFactory loggerFactory)
    {
        var opts = options.Value;
        var logger = loggerFactory.CreateLogger("EventBus.RelationDb");
        var freeSql = new FreeSqlBuilder()
            .UseConnectionString(opts.DataType, opts.ConnectionString)
            .UseMonitorCommand(cmd => logger.LogDebug($"Sql: {cmd.CommandText}"))
            .UseNameConvert(NameConvertType.PascalCaseToUnderscoreWithLower)
            .Build();

        var schemaPrefix = options.Value.Schema;
        if (!string.IsNullOrWhiteSpace(schemaPrefix))
            schemaPrefix = $"{schemaPrefix}.";

        freeSql.Aop.ConfigEntity += (_, e) =>
        {
            if (e.EntityType == typeof(RelationDbMessageStoragePublishedModel))
                e.ModifyResult.Name = $"{schemaPrefix}{opts.PublishedTableName}";
            else if (e.EntityType == typeof(RelationDbMessageStorageReceivedModel))
                e.ModifyResult.Name = $"{schemaPrefix}{opts.ReceivedTableName}";
        };

        _freeSql = freeSql;
    }

    private readonly IFreeSql _freeSql;

    public IFreeSql Instance() => _freeSql;
}