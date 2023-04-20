using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.RelationDbStorage;

namespace Shashlik.EventBus.Sqlite;

public class SqliteMessageStorage : RelationDbMessageStorageBase
{
    public SqliteMessageStorage(IOptionsMonitor<EventBusSqliteOptions> options, IConnectionString connectionString)
    {
        Options = options;
        ConnectionString = connectionString;
    }

    private IOptionsMonitor<EventBusSqliteOptions> Options { get; }
    private IConnectionString ConnectionString { get; }

    protected override string SqlTagCharPrefix => "`";
    protected override string SqlTagCharSuffix => "`";

    protected override IDbConnection CreateConnection()
    {
        return new SqliteConnection(ConnectionString.ConnectionString);
    }

    protected override string PublishedTableName => Options.CurrentValue.PublishedTableName;
    protected override string ReceivedTableName => Options.CurrentValue.ReceivedTableName;

    protected override string ReturnInsertIdSql => @";
SELECT last_insert_rowid()";
}