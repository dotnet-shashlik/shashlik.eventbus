using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Shashlik.EventBus.PostgreSQL
{
    internal class PostgreSQLMessageStorageInitializer : IMessageStorageInitializer
    {
        public PostgreSQLMessageStorageInitializer(IOptionsMonitor<EventBusPostgreSQLOptions> options,
            IConnectionString connectionString)
        {
            Options = options;
            ConnectionString = connectionString;
        }

        private IOptionsMonitor<EventBusPostgreSQLOptions> Options { get; }
        private IConnectionString ConnectionString { get; }

        public async Task Initialize(CancellationToken cancellationToken = default)
        {
            // 9.2及以下不支持CREATE SCHEMA IF NOT EXISTS，只能使用public
            var sql = $@"
{(Options.CurrentValue.Schema == "public" ? "CREATE SCHEMA IF NOT EXISTS " + Options.CurrentValue.Schema + ";" : "")}

CREATE TABLE IF NOT EXISTS {Options.CurrentValue.FullPublishTableName}(
    ""id"" SERIAL PRIMARY KEY,    
    ""msgId"" varchar(32) COLLATE ""pg_catalog"".""default"" NOT NULL,
    ""environment"" varchar(32) COLLATE ""pg_catalog"".""default"",
    ""eventName"" varchar(255) COLLATE ""pg_catalog"".""default"" NOT NULL,
    ""eventBody"" text COLLATE ""pg_catalog"".""default"" NOT NULL,
    ""createTime"" int8 NOT NULL,
    ""delayAt"" int8 NOT NULL,
    ""expireTime"" int8 NOT NULL,
    ""eventItems"" text COLLATE ""pg_catalog"".""default"",
    ""status"" varchar(32) COLLATE ""pg_catalog"".""default"" NOT NULL,
    ""retryCount"" int4 NOT NULL,
    ""isLocking"" bool NOT NULL,
    ""lockEnd"" int8 NOT NULL
);
CREATE UNIQUE INDEX ""IX_published_msgId"" ON {Options.CurrentValue.FullPublishTableName} (""msgId"");

CREATE TABLE IF NOT EXISTS {Options.CurrentValue.FullReceiveTableName}(
    ""id"" SERIAL PRIMARY KEY,
    ""msgId"" varchar(32) COLLATE ""pg_catalog"".""default"" NOT NULL,
    ""environment"" varchar(32) COLLATE ""pg_catalog"".""default"",
    ""eventName"" varchar(255) COLLATE ""pg_catalog"".""default"" NOT NULL,
    ""eventHandlerName"" varchar(255) COLLATE ""pg_catalog"".""default"" NOT NULL,
    ""eventBody"" text COLLATE ""pg_catalog"".""default"" NOT NULL,
    ""createTime"" int8 NOT NULL,
	""isDelay"" bool NOT NULL,
    ""delayAt"" int8 NOT NULL,
    ""expireTime"" int8 NOT NULL,
    ""eventItems"" text COLLATE ""pg_catalog"".""default"",
    ""status"" varchar(32) COLLATE ""pg_catalog"".""default"" NOT NULL,
    ""retryCount"" int4 NOT NULL,
    ""isLocking"" bool NOT NULL,
    ""lockEnd"" int8 NOT NULL
);
CREATE UNIQUE INDEX ""IX_received_msgId_eventHandlerName"" ON {Options.CurrentValue.FullReceiveTableName} (""msgId"", ""eventHandlerName"");
";

            await using var connection = new NpgsqlConnection(ConnectionString.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}