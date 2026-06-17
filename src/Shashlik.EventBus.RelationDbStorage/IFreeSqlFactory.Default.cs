using System;
using FreeSql;
using FreeSql.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.RelationDbStorage;

/// <summary>
/// <see cref="IFreeSqlFactory"/> 的默认实现:根据 <see cref="EventBusRelationDbOptions.DataType"/>
/// 选用对应的 FreeSql Provider,所有方言(MySQL/PG/SqlServer/Sqlite/...)走同一份代码。
/// </summary>
internal class DefaultFreeSqlFactory : IFreeSqlFactory, IDisposable
{
    public DefaultFreeSqlFactory(
        IOptions<EventBusRelationDbOptions> options,
        ILoggerFactory loggerFactory,
        IServiceProvider serviceProvider)
    {
        var opts = options.Value;
        var logger = loggerFactory.CreateLogger("EventBus.RelationDb");
        var freeSqlBuilder = new FreeSqlBuilder();
        if (opts.ConnectionFactory is not null)
        {
            var connectionFactory = serviceProvider.GetService(opts.ConnectionFactory) as IConnectionFactory;
            if (connectionFactory is null)
                throw new OptionsValidationException("ConnectionFactory", opts.GetType(),
                    ["Invalid connection factory"]);
            freeSqlBuilder.UseConnectionString(connectionFactory.DataType, connectionFactory.ConnectionString);
        }
        else if (!string.IsNullOrWhiteSpace(opts.ConnectionString) && opts.DataType.HasValue)
        {
            freeSqlBuilder.UseConnectionString(opts.DataType.Value, opts.ConnectionString);
        }
        else
        {
            throw new OptionsValidationException("ConnectionString", opts.GetType(), ["Invalid connection string"]);
        }

        var freeSql = freeSqlBuilder
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

    public void Dispose()
    {
        _freeSql.Dispose();
    }
}