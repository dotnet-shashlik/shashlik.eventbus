using System.Data;
using Microsoft.Extensions.Options;
using Npgsql;
using Shashlik.EventBus.RelationDbStorage;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.PostgreSQL;

public class PostgreSQLMessageStorage : RelationDbMessageStorageBase
{
    public PostgreSQLMessageStorage(IOptionsMonitor<EventBusPostgreSQLOptions> options,
        IConnectionString connectionString)
    {
        Options = options;
        ConnectionString = connectionString;
    }

    private IOptionsMonitor<EventBusPostgreSQLOptions> Options { get; }
    private IConnectionString ConnectionString { get; }


    protected override IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(ConnectionString.ConnectionString);
    }

    protected override string PublishedTableName => Options.CurrentValue.FullPublishedTableName;
    protected override string ReceivedTableName => Options.CurrentValue.FullReceivedTableName;
    protected override string ReturnInsertIdSql => " RETURNING id";

    protected override string SqlTagCharPrefix => "\"";
    protected override string SqlTagCharSuffix => "\"";
    protected override string BoolTrueValue => "true";
    protected override string BoolFalseValue => "false";

    protected override object ToSaveObject(MessageStorageModel model)
    {
        return new
        {
            Id = model.Id,
            MsgId = model.MsgId,
            Environment = model.Environment,
            CreateTime = model.CreateTime.GetLongDate(),
            DelayAt = model.DelayAt?.GetLongDate() ?? 0,
            ExpireTime = model.ExpireTime?.GetLongDate() ?? 0,
            EventHandlerName = model.EventHandlerName,
            EventName = model.EventName,
            EventBody = model.EventBody,
            EventItems = model.EventItems,
            RetryCount = model.RetryCount,
            Status = model.Status,
            IsLocking = model.IsLocking,
            LockEnd = model.LockEnd?.GetLongDate() ?? 0,
            IsDelay = model.DelayAt.HasValue
        };
    }
}