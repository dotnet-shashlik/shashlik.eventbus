using System.Data;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Shashlik.EventBus.RelationDbStorage;

namespace Shashlik.EventBus.MySql;

public class MySqlMessageStorage : RelationDbMessageStorageBase
{
    public MySqlMessageStorage(IOptionsMonitor<EventBusMySqlOptions> options, IConnectionString connectionString)
    {
        Options = options;
        ConnectionString = connectionString;
    }

    private IOptionsMonitor<EventBusMySqlOptions> Options { get; }
    private IConnectionString ConnectionString { get; }


    protected override IDbConnection CreateConnection()
    {
        return new MySqlConnection(ConnectionString.ConnectionString);
    }

    protected override string PublishedTableName => Options.CurrentValue.PublishedTableName;
    protected override string ReceivedTableName => Options.CurrentValue.ReceivedTableName;
    protected override string SqlTagCharPrefix => "`";
    protected override string SqlTagCharSuffix => "`";
    protected override string ReturnInsertIdSql => @";
SELECT LAST_INSERT_ID()";
}