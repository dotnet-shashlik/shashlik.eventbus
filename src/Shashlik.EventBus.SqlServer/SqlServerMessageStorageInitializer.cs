using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.SqlServer
{
    public class SqlServerMessageStorageInitializer : IMessageStorageInitializer
    {
        public SqlServerMessageStorageInitializer(IOptionsMonitor<EventBusSqlServerOptions> options,
            IConnectionString connectionString)
        {
            Options = options;
            ConnectionString = connectionString;
        }

        private IOptionsMonitor<EventBusSqlServerOptions> Options { get; }
        private IConnectionString ConnectionString { get; }

        public async Task Initialize(CancellationToken cancellationToken = default)
        {
            var sql = $@"
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{Options.CurrentValue.Schema}')
BEGIN
	EXEC('CREATE SCHEMA [{Options.CurrentValue.Schema}]')
END;

IF OBJECT_ID(N'{Options.CurrentValue.FullPublishedTableName}',N'U') IS NULL
BEGIN
	CREATE TABLE {Options.CurrentValue.FullPublishedTableName}
	(
		[id] BIGINT IDENTITY(1,1) PRIMARY KEY NOTNULL,
		[msgId] VARCHAR(32) NOT NULL,
		[environment] VARCHAR(32),
		[eventName] VARCHAR(255) NOT NULL,
		[eventBody] NVARCHAR(MAX) NOT NULL,
		[createTime] BIGINT NOT NULL,
		[delayAt] BIGINT NOT NULL,
		[expireTime] BIGINT NOT NULL,
		[eventItems] NVARCHAR(MAX) NULL,
		[status] VARCHAR(32) NOT NULL,
		[retryCount] INT NOT NULL,
		[isLocking] BIT NOT NULL,
		[lockEnd] BIGINT NOT NULL,
		UNIQUE INDEX [IX_published_msgId] ([msgId])
	);
END;

IF OBJECT_ID(N'{Options.CurrentValue.FullReceivedTableName}',N'U') IS NULL
BEGIN
	CREATE TABLE {Options.CurrentValue.FullReceivedTableName}
	(
		[id] BIGINT IDENTITY(1,1) PRIMARY KEY NOTNULL,
		[msgId] VARCHAR(32) NOT NULL,
		[environment] VARCHAR(32),
		[eventName] VARCHAR(255) NOT NULL,
		[eventHandlerName] VARCHAR(255) NOT NULL,
		[eventBody] NVARCHAR(MAX) NOT NULL,
		[createTime] BIGINT NOT NULL,
		[isDelay] BIT NOT NULL,
		[delayAt] BIGINT NOT NULL,
		[expireTime] BIGINT NOT NULL,
		[eventItems] NVARCHAR(MAX) NULL,
		[status] VARCHAR(32) NOT NULL,
		[retryCount] INT NOT NULL,
		[isLocking] BIT NOT NULL,
		[lockEnd] BIGINT NOT NULL,
		UNIQUE INDEX [IX_received_msgId_eventHandlerName] ([msgId], [eventHandlerName])
	);
END;
";
            await using var connection = new SqlConnection(ConnectionString.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}