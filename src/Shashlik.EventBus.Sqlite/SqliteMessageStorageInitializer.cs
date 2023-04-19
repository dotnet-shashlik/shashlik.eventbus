using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.Sqlite
{
    public class SqliteMessageStorageInitializer : IMessageStorageInitializer
    {
        public SqliteMessageStorageInitializer(IOptionsMonitor<EventBusSqliteOptions> options,
            IConnectionString connectionString)
        {
            Options = options;
            ConnectionString = connectionString;
        }

        private IOptionsMonitor<EventBusSqliteOptions> Options { get; }
        private IConnectionString ConnectionString { get; }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            var sql = $@"
CREATE TABLE IF NOT EXISTS {Options.CurrentValue.PublishedTableName}
(
	id INTEGER PRIMARY KEY AUTOINCREMENT,
	msgId TEXT NOT NULL,
	environment TEXT,
	eventName TEXT NOT NULL,
	eventBody TEXT NOT NULL,
	createTime INTEGER NOT NULL,
	delayAt INTEGER NOT NULL,
	expireTime INTEGER NOT NULL,
	eventItems TEXT NULL,
	status TEXT NOT NULL,
	retryCount INTEGER NOT NULL,
	isLocking INTEGER NOT NULL,
	lockEnd INTEGER NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS  INDEX_{Options.CurrentValue.PublishedTableName}_msgId ON {Options.CurrentValue.PublishedTableName} (msgId);


CREATE TABLE IF NOT EXISTS {Options.CurrentValue.ReceivedTableName}
(
	id INTEGER PRIMARY KEY AUTOINCREMENT,
	msgId TEXT NOT NULL,
	environment TEXT,
	eventName TEXT NOT NULL,
	eventHandlerName TEXT NOT NULL,
	eventBody TEXT NOT NULL,
	createTime INTEGER NOT NULL,
	isDelay INTEGER NOT NULL,
	delayAt INTEGER NOT NULL,
	expireTime INTEGER NOT NULL,
	eventItems TEXT NULL,
	status TEXT NOT NULL,
	retryCount INTEGER NOT NULL,
	isLocking INTEGER NOT NULL,
	lockEnd INTEGER NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS  INDEX_{Options.CurrentValue.ReceivedTableName}_msgId ON {Options.CurrentValue.ReceivedTableName} (msgId, eventHandlerName);
";

            await using var connection = new SqliteConnection(ConnectionString.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}