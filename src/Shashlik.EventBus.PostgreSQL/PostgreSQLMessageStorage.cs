using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.PostgreSQL
{
    public class PostgreSQLMessageStorage : IMessageStorage
    {
        public PostgreSQLMessageStorage(IOptionsMonitor<EventBusPostgreSQLOptions> options,
            IConnectionString connectionString, ILogger<PostgreSQLMessageStorage> logger)
        {
            Options = options;
            ConnectionString = connectionString;
            Logger = logger;
        }

        private IOptionsMonitor<EventBusPostgreSQLOptions> Options { get; }
        private IConnectionString ConnectionString { get; }
        private ILogger<PostgreSQLMessageStorage> Logger { get; }

        public async ValueTask<bool> ExistsPublishMessage(string msgId, CancellationToken cancellationToken = default)
        {
            var sql =
                $"SELECT COUNT(\"msgId\") FROM {Options.CurrentValue.FullPublishTableName} WHERE \"msgId\"='{msgId}';";

            var count = (await SqlScalar(sql, cancellationToken).ConfigureAwait(false))?.ParseTo<int>() ?? 0;
            return count > 0;
        }

        public async ValueTask<bool> ExistsReceiveMessage(string msgId, CancellationToken cancellationToken = default)
        {
            var sql =
                $"SELECT COUNT(\"msgId\") FROM {Options.CurrentValue.FullReceiveTableName} WHERE \"msgId\"='{msgId}';";

            var count = (await SqlScalar(sql, cancellationToken).ConfigureAwait(false))?.ParseTo<int>() ?? 0;
            return count > 0;
        }

        public async Task<MessageStorageModel> FindPublishedById(string id,
            CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT * FROM {Options.CurrentValue.FullPublishTableName} WHERE \"msgId\"='{id}';";

            var table = await SqlQuery(sql, cancellationToken).ConfigureAwait(false);
            if (table.Rows.Count == 0)
                return null;

            return new MessageStorageModel
            {
                MsgId = table.Rows[0].GetValue<string>("msgId"),
                Environment = table.Rows[0].GetValue<string>("environment"),
                CreateTime = table.Rows[0].GetValue<long>("createTime").LongToDateTimeOffset(),
                DelayAt = table.Rows[0].GetValue<long?>("delayAt")?.LongToDateTimeOffset(),
                ExpireTime = table.Rows[0].GetValue<long?>("expireTime")?.LongToDateTimeOffset(),
                EventName = table.Rows[0].GetValue<string>("eventName"),
                EventBody = table.Rows[0].GetValue<string>("eventBody"),
                EventItems = table.Rows[0].GetValue<string>("eventItems"),
                RetryCount = table.Rows[0].GetValue<int>("retryCount"),
                Status = table.Rows[0].GetValue<string>("status"),
                IsLocking = table.Rows[0].GetValue<bool>("isLocking"),
                LockEnd = table.Rows[0].GetValue<long?>("lockEnd")?.LongToDateTimeOffset()
            };
        }

        public async Task<MessageStorageModel> FindReceivedById(string id,
            CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT * FROM {Options.CurrentValue.FullReceiveTableName} WHERE \"msgId\"='{id}';";

            var table = await SqlQuery(sql, cancellationToken).ConfigureAwait(false);
            if (table.Rows.Count == 0)
                return null;

            return new MessageStorageModel
            {
                MsgId = table.Rows[0].GetValue<string>("msgId"),
                Environment = table.Rows[0].GetValue<string>("environment"),
                CreateTime = table.Rows[0].GetValue<long>("createTime").LongToDateTimeOffset(),
                DelayAt = table.Rows[0].GetValue<long?>("delayAt")?.LongToDateTimeOffset(),
                ExpireTime = table.Rows[0].GetValue<long?>("expireTime")?.LongToDateTimeOffset(),
                EventName = table.Rows[0].GetValue<string>("eventName"),
                EventHandlerName = table.Rows[0].GetValue<string>("eventHandlerName"),
                EventBody = table.Rows[0].GetValue<string>("eventBody"),
                EventItems = table.Rows[0].GetValue<string>("eventItems"),
                RetryCount = table.Rows[0].GetValue<int>("retryCount"),
                Status = table.Rows[0].GetValue<string>("status"),
                IsLocking = table.Rows[0].GetValue<bool>("isLocking"),
                LockEnd = table.Rows[0].GetValue<long?>("lockEnd")?.LongToDateTimeOffset()
            };
        }

        public async Task SavePublished(MessageStorageModel message, TransactionContext transactionContext,
            CancellationToken cancellationToken = default)
        {
            var sql = $@"
INSERT INTO {Options.CurrentValue.FullPublishTableName}
(""msgId"", ""environment"", ""createTime"", ""delayAt"", ""expireTime"", ""eventName"", ""eventBody"", ""eventItems"", ""retryCount"", ""status"", ""isLocking"", ""lockEnd"")
VALUES(@msgId, @environment, @createTime, @delayAt, @expireTime, @eventName, @eventBody, @eventItems, @retryCount, @status, @isLocking, @lockEnd);
";

            var parameters = new[]
            {
                new NpgsqlParameter("@msgId", NpgsqlDbType.Varchar) {Value = message.MsgId},
                new NpgsqlParameter("@environment", NpgsqlDbType.Varchar) {Value = message.Environment},
                new NpgsqlParameter("@createTime", NpgsqlDbType.Bigint) {Value = message.CreateTime.GetLongDate()},
                new NpgsqlParameter("@delayAt", NpgsqlDbType.Bigint) {Value = message.DelayAt?.GetLongDate() ?? 0},
                new NpgsqlParameter("@expireTime", NpgsqlDbType.Bigint)
                    {Value = message.ExpireTime?.GetLongDate() ?? 0},
                new NpgsqlParameter("@eventName", NpgsqlDbType.Varchar) {Value = message.EventName},
                new NpgsqlParameter("@eventBody", NpgsqlDbType.Text) {Value = message.EventBody},
                new NpgsqlParameter("@eventItems", NpgsqlDbType.Text) {Value = message.EventItems},
                new NpgsqlParameter("@retryCount", NpgsqlDbType.Integer) {Value = message.RetryCount},
                new NpgsqlParameter("@status", NpgsqlDbType.Varchar) {Value = message.Status},
                new NpgsqlParameter("@isLocking", NpgsqlDbType.Boolean) {Value = message.IsLocking},
                new NpgsqlParameter("@lockEnd", NpgsqlDbType.Bigint) {Value = message.LockEnd?.GetLongDate() ?? 0},
            };

            var row = await NonQuery(transactionContext, sql, parameters, cancellationToken).ConfigureAwait(false);
            if (row == 0)
                throw new DbUpdateException();
        }

        public async Task SaveReceived(MessageStorageModel message, CancellationToken cancellationToken = default)
        {
            var sql = $@"
INSERT INTO {Options.CurrentValue.FullReceiveTableName}
(""msgId"", ""environment"", ""createTime"", ""isDelay"", ""delayAt"", ""expireTime"", ""eventName"", ""eventHandlerName"", ""eventBody"", ""eventItems"", ""retryCount"", ""status"", ""isLocking"", ""lockEnd"")
VALUES(@msgId, @environment, @createTime, @isDelay, @delayAt, @expireTime, @eventName, @eventHandlerName, @eventBody, @eventItems, @retryCount, @status, @isLocking, @lockEnd);
";

            var parameters = new[]
            {
                new NpgsqlParameter("@msgId", NpgsqlDbType.Varchar) {Value = message.MsgId},
                new NpgsqlParameter("@environment", NpgsqlDbType.Varchar) {Value = message.Environment},
                new NpgsqlParameter("@createTime", NpgsqlDbType.Bigint) {Value = message.CreateTime.GetLongDate()},
                new NpgsqlParameter("@isDelay", NpgsqlDbType.Boolean) {Value = message.DelayAt.HasValue},
                new NpgsqlParameter("@delayAt", NpgsqlDbType.Bigint) {Value = message.DelayAt?.GetLongDate() ?? 0},
                new NpgsqlParameter("@expireTime", NpgsqlDbType.Bigint)
                    {Value = message.ExpireTime?.GetLongDate() ?? 0},
                new NpgsqlParameter("@eventName", NpgsqlDbType.Varchar) {Value = message.EventName},
                new NpgsqlParameter("@eventHandlerName", NpgsqlDbType.Varchar) {Value = message.EventHandlerName},
                new NpgsqlParameter("@eventBody", NpgsqlDbType.Text) {Value = message.EventBody},
                new NpgsqlParameter("@eventItems", NpgsqlDbType.Text) {Value = message.EventItems},
                new NpgsqlParameter("@retryCount", NpgsqlDbType.Integer) {Value = message.RetryCount},
                new NpgsqlParameter("@status", NpgsqlDbType.Varchar) {Value = message.Status},
                new NpgsqlParameter("@isLocking", NpgsqlDbType.Boolean) {Value = message.IsLocking},
                new NpgsqlParameter("@lockEnd", NpgsqlDbType.Bigint) {Value = message.LockEnd?.GetLongDate() ?? 0},
            };

            var row = await NonQuery(sql, parameters, cancellationToken).ConfigureAwait(false);
            if (row == 0)
                throw new DbUpdateException();
        }

        public async Task UpdatePublished(string msgId, string status, int retryCount, DateTimeOffset? expireTime,
            CancellationToken cancellationToken = default)
        {
            var sql = $@"
UPDATE {Options.CurrentValue.FullPublishTableName}
SET ""status"" = '{status}', ""retryCount"" = {retryCount}, ""expireTime"" = {expireTime?.GetLongDate() ?? 0}
WHERE ""msgId"" = '{msgId}'
";

            await NonQuery(sql, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateReceived(string msgId, string status, int retryCount, DateTimeOffset? expireTime,
            CancellationToken cancellationToken = default)
        {
            var sql = $@"
UPDATE {Options.CurrentValue.FullReceiveTableName}
SET ""status"" = '{status}', ""retryCount"" = {retryCount}, ""expireTime"" = {expireTime?.GetLongDate() ?? 0}
WHERE ""msgId"" = '{msgId}'
";
            await NonQuery(sql, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> TryLockReceived(string msgId, DateTimeOffset lockEndAt,
            CancellationToken cancellationToken)
        {
            if (lockEndAt <= DateTimeOffset.Now)
                throw new ArgumentOutOfRangeException(nameof(lockEndAt));
            var nowLong = DateTime.Now.GetLongDate();

            var sql = $@"
UPDATE {Options.CurrentValue.FullReceiveTableName}
SET ""isLocking"" = true, ""lockEnd"" = {lockEndAt.GetLongDate()}
WHERE ""msgId"" = '{msgId}' AND (""isLocking"" = false OR ""lockEnd"" < {nowLong})
";
            try
            {
                return await NonQuery(sql, null, cancellationToken).ConfigureAwait(false) == 1;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"[EventBus] TryLockReceived error.");
                return false;
            }
        }

        public async Task DeleteExpires(CancellationToken cancellationToken = default)
        {
            var now = DateTime.Now.GetLongDate();
            var sql = $@"
DELETE FROM {Options.CurrentValue.FullPublishTableName} WHERE ""expireTime"" != 0 AND ""expireTime"" < {now} AND ""status"" != '{MessageStatus.Scheduled}';
DELETE FROM {Options.CurrentValue.FullReceiveTableName} WHERE ""expireTime"" != 0 AND ""expireTime"" < {now} AND ""status"" != '{MessageStatus.Scheduled}';
";
            await NonQuery(sql, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task<List<MessageStorageModel>> GetPublishedMessagesOfNeedRetryAndLock(int count,
            int delayRetrySecond, int maxFailedRetryCount,
            string environment, int lockSecond, CancellationToken cancellationToken = default)
        {
            var createTimeLimit = DateTime.Now.AddSeconds(-delayRetrySecond).GetLongDate();
            var now = DateTime.Now;
            var nowLong = now.GetLongDate();

            var sql = $@"
SELECT * FROM {Options.CurrentValue.FullPublishTableName}
WHERE
    ""environment"" = '{environment}'
    AND ""createTime"" < {createTimeLimit}
    AND ""retryCount"" < {maxFailedRetryCount}
    AND (""isLocking"" = false OR ""lockEnd"" < {nowLong})
    AND (""status"" = '{MessageStatus.Scheduled}' OR ""status"" = '{MessageStatus.Failed}')
LIMIT {count};
";

            var table = await SqlQuery(sql, cancellationToken).ConfigureAwait(false);
            if (table.Rows.Count == 0) return new List<MessageStorageModel>();
            var idsBuilder = new StringBuilder();
            var list = table.AsEnumerable()
                .Select(row =>
                {
                    var msgId = row.GetValue<string>("msgId");

                    idsBuilder.Append("'");
                    idsBuilder.Append(msgId);
                    idsBuilder.Append("'");
                    idsBuilder.Append(",");

                    return new MessageStorageModel
                    {
                        MsgId = msgId,
                        Environment = row.GetValue<string>("environment"),
                        CreateTime = row.GetValue<long>("createTime").LongToDateTimeOffset(),
                        DelayAt = row.GetValue<long?>("delayAt")?.LongToDateTimeOffset(),
                        ExpireTime = row.GetValue<long?>("expireTime")?.LongToDateTimeOffset(),
                        EventName = row.GetValue<string>("eventName"),
                        EventBody = row.GetValue<string>("eventBody"),
                        EventItems = row.GetValue<string>("eventItems"),
                        RetryCount = row.GetValue<int>("retryCount"),
                        Status = row.GetValue<string>("status"),
                        IsLocking = row.GetValue<bool>("isLocking"),
                        LockEnd = row.GetValue<long?>("lockEnd")?.LongToDateTimeOffset()
                    };
                }).ToList();
            var ids = idsBuilder.ToString();
            ids = ids.AsSpan()[0..(ids.Length - 1)].ToString();

            var lockEnd = now.AddSeconds(lockSecond).GetLongDate();
            var updateSql = $@"
UPDATE {Options.CurrentValue.FullPublishTableName}
SET ""isLocking"" = true, ""lockEnd"" = {lockEnd}
WHERE ""msgId"" IN ({ids}) AND (""isLocking"" = false OR ""lockEnd"" < {nowLong});
";
            var rows = await NonQuery(updateSql, null, cancellationToken).ConfigureAwait(false);
            return rows != list.Count ? new List<MessageStorageModel>() : list;
        }

        public async Task<List<MessageStorageModel>> GetReceivedMessagesOfNeedRetryAndLock(int count,
            int delayRetrySecond, int maxFailedRetryCount, string environment,
            int lockSecond, CancellationToken cancellationToken = default)
        {
            var createTimeLimit = DateTime.Now.AddSeconds(-delayRetrySecond).GetLongDate();
            var now = DateTime.Now;
            var nowLong = now.GetLongDate();

            var sql = $@"
SELECT * FROM {Options.CurrentValue.FullReceiveTableName}
WHERE
    ""environment"" = '{environment}'
    AND ((""isDelay"" = false AND ""createTime"" < {createTimeLimit}) OR (""isDelay"" = true AND ""delayAt"" <= {nowLong} ))
    AND ""retryCount"" < {maxFailedRetryCount}
    AND (""isLocking"" = false OR ""lockEnd"" < {nowLong})
    AND (""status"" = '{MessageStatus.Scheduled}' OR ""status"" = '{MessageStatus.Failed}')
LIMIT {count};
";

            var table = await SqlQuery(sql, cancellationToken).ConfigureAwait(false);
            if (table.Rows.Count == 0) return new List<MessageStorageModel>();
            var idsBuilder = new StringBuilder();
            var list = table.AsEnumerable()
                .Select(row =>
                {
                    var msgId = row.GetValue<string>("msgId");

                    idsBuilder.Append("'");
                    idsBuilder.Append(msgId);
                    idsBuilder.Append("'");
                    idsBuilder.Append(",");

                    return new MessageStorageModel
                    {
                        MsgId = msgId,
                        Environment = row.GetValue<string>("environment"),
                        CreateTime = row.GetValue<long>("createTime").LongToDateTimeOffset(),
                        DelayAt = row.GetValue<long?>("delayAt")?.LongToDateTimeOffset(),
                        ExpireTime = row.GetValue<long?>("expireTime")?.LongToDateTimeOffset(),
                        EventName = row.GetValue<string>("eventName"),
                        EventHandlerName = row.GetValue<string>("eventHandlerName"),
                        EventBody = row.GetValue<string>("eventBody"),
                        EventItems = row.GetValue<string>("eventItems"),
                        RetryCount = row.GetValue<int>("retryCount"),
                        Status = row.GetValue<string>("status"),
                        IsLocking = row.GetValue<bool>("isLocking"),
                        LockEnd = row.GetValue<long?>("lockEnd")?.LongToDateTimeOffset()
                    };
                }).ToList();
            var ids = idsBuilder.ToString();
            ids = ids.AsSpan()[0..(ids.Length - 1)].ToString();

            var lockEnd = now.AddSeconds(lockSecond).GetLongDate();
            var updateSql = $@"
UPDATE {Options.CurrentValue.FullReceiveTableName}
SET ""isLocking"" = true, ""lockEnd"" = {lockEnd}
WHERE ""msgId"" IN ({ids}) AND (""isLocking"" = false OR ""lockEnd"" < {nowLong});
";
            var rows = await NonQuery(updateSql, null, cancellationToken).ConfigureAwait(false);
            return rows != list.Count ? new List<MessageStorageModel>() : list;
        }

        private async Task<DataTable> SqlQuery(string sql, CancellationToken cancellationToken = default)
        {
            await using var connection = new NpgsqlConnection(ConnectionString.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            var table = new DataTable();
            table.Load(reader);
            return table;
        }

        private async Task<object> SqlScalar(string sql, CancellationToken cancellationToken = default)
        {
            await using var connection = new NpgsqlConnection(ConnectionString.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            return await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<int> NonQuery(string sql, NpgsqlParameter[] parameter,
            CancellationToken cancellationToken = default)
        {
            await using var connection = new NpgsqlConnection(ConnectionString.ConnectionString);
            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            if (!parameter.IsNullOrEmpty())
                foreach (var mySqlParameter in parameter)
                    cmd.Parameters.Add(mySqlParameter);
            cmd.CommandText = sql;
            return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<int> NonQuery(TransactionContext transactionContext, string sql, NpgsqlParameter[] parameter,
            CancellationToken cancellationToken = default)
        {
            if (transactionContext == null)
                return await NonQuery(sql, parameter, cancellationToken).ConfigureAwait(false);
            else if (transactionContext.ConnectionInstance is DbContext dbContext)
            {
                var connection = dbContext.Database.GetDbConnection();
                if (connection.State == ConnectionState.Closed)
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                await using var cmd = connection.CreateCommand();
                if (!parameter.IsNullOrEmpty())
                    foreach (var mySqlParameter in parameter)
                        cmd.Parameters.Add(mySqlParameter);

                cmd.CommandText = sql;

                if (transactionContext.TransactionInstance == null)
                {
                    if (dbContext.Database.CurrentTransaction != null)
                        cmd.Transaction = dbContext.Database.CurrentTransaction.GetDbTransaction();
                }
                else if (transactionContext.TransactionInstance is IDbContextTransaction dbContextTransaction)
                    cmd.Transaction = dbContextTransaction.GetDbTransaction();
                else if (transactionContext.TransactionInstance is IDbTransaction dbTransaction)
                    cmd.Transaction = dbTransaction as DbTransaction;
                else
                    throw new InvalidCastException(
                        $"[EventBus] invalid transaction context data. you can use DbContext or DbConnection for ConnectionInstance, and use IDbContextTransaction or IDbTransaction for TransactionInstance.");

                return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            else if (transactionContext.ConnectionInstance is DbConnection connection)
            {
                if (connection.State == ConnectionState.Closed)
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                await using var cmd = connection.CreateCommand();
                if (!parameter.IsNullOrEmpty())
                    foreach (var mySqlParameter in parameter)
                        cmd.Parameters.Add(mySqlParameter);
                cmd.CommandText = sql;

                if (transactionContext.TransactionInstance is DbTransaction dbTransaction)
                    cmd.Transaction = dbTransaction;
                else
                    throw new InvalidCastException(
                        $"[EventBus] invalid transaction context data. you can use DbContext or DbConnection for ConnectionInstance, and use IDbContextTransaction or IDbTransaction for TransactionInstance.");

                return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            throw new InvalidCastException(
                $"[EventBus] invalid transaction context data. you can use DbContext or DbConnection for ConnectionInstance, and use IDbContextTransaction or IDbTransaction for TransactionInstance.");
        }
    }
}