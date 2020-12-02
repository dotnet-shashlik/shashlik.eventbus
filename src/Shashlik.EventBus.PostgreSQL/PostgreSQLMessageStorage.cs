using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Shashlik.EventBus.RelationDbStorage;
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

        public async ValueTask<bool> TransactionIsCommitted(string msgId, ITransactionContext? transactionContext,
            CancellationToken cancellationToken = default)
        {
            if (transactionContext != null)
            {
                if (!(transactionContext is RelationDbStorageTransactionContext relationDbStorageTransactionContext))
                    throw new InvalidCastException(
                        $"[EventBus-PostgreSql]Storage only support transaction context of {typeof(RelationDbStorageTransactionContext)}");
                // 事务的连接的信息未null了表示事务已回滚回已提交
                if (relationDbStorageTransactionContext.DbTransaction.Connection != null)
                    return false;
            }

            var sql =
                $"SELECT COUNT(\"msgId\") FROM {Options.CurrentValue.FullPublishedTableName} WHERE \"msgId\"='{msgId}';";

            var count = (await SqlScalar(sql, cancellationToken).ConfigureAwait(false))?.ParseTo<int>() ?? 0;
            return count > 0;
        }

        public async Task<MessageStorageModel?> FindPublishedByMsgId(string msgId,
            CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM {Options.CurrentValue.FullPublishedTableName} WHERE \"msgId\"='{msgId}';";

            var table = await SqlQuery(sql, null, cancellationToken).ConfigureAwait(false);
            if (table.Rows.Count == 0)
                return null;

            return RowToPublishedModel(table.Rows[0]);
        }

        public async Task<MessageStorageModel?> FindPublishedById(long id, CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM {Options.CurrentValue.FullPublishedTableName} WHERE \"id\"={id};";

            var table = await SqlQuery(sql, null, cancellationToken).ConfigureAwait(false);
            if (table.Rows.Count == 0)
                return null;
            return RowToPublishedModel(table.Rows[0]);
        }

        public async Task<MessageStorageModel?> FindReceivedByMsgId(string msgId, EventHandlerDescriptor eventHandlerDescriptor,
            CancellationToken cancellationToken = default)
        {
            var sql =
                $"SELECT * FROM {Options.CurrentValue.FullReceivedTableName} WHERE \"msgId\"='{msgId}' AND \"eventHandlerName\"='{eventHandlerDescriptor.EventHandlerName}';";

            var table = await SqlQuery(sql, null, cancellationToken).ConfigureAwait(false);
            if (table.Rows.Count == 0)
                return null;

            return RowToReceivedModel(table.Rows[0]);
        }

        public async Task<MessageStorageModel?> FindReceivedById(long id, CancellationToken cancellationToken)
        {
            var sql =
                $"SELECT * FROM {Options.CurrentValue.FullReceivedTableName} WHERE \"id\"={id};";

            var table = await SqlQuery(sql, null, cancellationToken).ConfigureAwait(false);
            if (table.Rows.Count == 0)
                return null;

            return RowToReceivedModel(table.Rows[0]);
        }

        public async Task<List<MessageStorageModel>> SearchPublished(string eventName, string status, int skip, int take,
            CancellationToken cancellationToken)
        {
            var where = new StringBuilder();
            if (!eventName.IsNullOrWhiteSpace())
                where.Append(" AND \"eventName\"=@eventName");
            if (!status.IsNullOrWhiteSpace())
                where.Append(" AND \"status\"=@status");

            var sql = $@"
SELECT * FROM {Options.CurrentValue.FullPublishedTableName}
WHERE
    1 = 1{where}
            ORDER BY ""createTime"" DESC
LIMIT {take} OFFSET {skip};
            ";

            var parameters = new[]
            {
                new NpgsqlParameter("@eventName", NpgsqlDbType.Varchar) {Value = eventName},
                new NpgsqlParameter("@status", NpgsqlDbType.Varchar) {Value = status},
            };

            var table = await SqlQuery(sql, parameters, cancellationToken).ConfigureAwait(false);
            if (table.Rows.Count == 0)
                return new List<MessageStorageModel>();
            return table.AsEnumerable()
                .Select(RowToPublishedModel)
                .ToList();
        }

        public async Task<List<MessageStorageModel>> SearchReceived(string eventName, string eventHandlerName, string status, int skip, int take,
            CancellationToken cancellationToken)
        {
            var where = new StringBuilder();
            if (!eventName.IsNullOrWhiteSpace())
                where.Append(" AND \"eventName\"=@eventName");
            if (!eventHandlerName.IsNullOrWhiteSpace())
                where.Append(" AND \"eventHandlerName\"=@eventHandlerName");
            if (!status.IsNullOrWhiteSpace())
                where.Append(" AND \"status\"=@status");

            var sql = $@"
SELECT * FROM {Options.CurrentValue.FullReceivedTableName}
WHERE
    1 = 1{where}
            ORDER BY ""createTime"" DESC
LIMIT {take} OFFSET {skip};
            ";
            var parameters = new[]
            {
                new NpgsqlParameter("@eventName", NpgsqlDbType.Varchar) {Value = eventName},
                new NpgsqlParameter("@eventHandlerName", NpgsqlDbType.Varchar) {Value = eventHandlerName},
                new NpgsqlParameter("@status", NpgsqlDbType.Varchar) {Value = status},
            };

            var table = await SqlQuery(sql, parameters, cancellationToken).ConfigureAwait(false);
            if (table.Rows.Count == 0)
                return new List<MessageStorageModel>();
            return table.AsEnumerable()
                .Select(RowToPublishedModel)
                .ToList();
        }

        public async Task<long> SavePublished(MessageStorageModel message, ITransactionContext? transactionContext,
            CancellationToken cancellationToken = default)
        {
            var sql = $@"
INSERT INTO {Options.CurrentValue.FullPublishedTableName}
(""msgId"", ""environment"", ""createTime"", ""delayAt"", ""expireTime"", ""eventName"", ""eventBody"", ""eventItems"", ""retryCount"", ""status"", ""isLocking"", ""lockEnd"")
VALUES(@msgId, @environment, @createTime, @delayAt, @expireTime, @eventName, @eventBody, @eventItems, @retryCount, @status, @isLocking, @lockEnd) RETURNING id;
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

            var id = await SqlScalar(transactionContext, sql, parameters, cancellationToken).ConfigureAwait(false);
            if (id is null || !id.TryParse<long>(out var longId))
                throw new DbUpdateException();

            message.Id = longId;
            return longId;
        }

        public async Task<long> SaveReceived(MessageStorageModel message, CancellationToken cancellationToken = default)
        {
            var sql = $@"
INSERT INTO {Options.CurrentValue.FullReceivedTableName}
(""msgId"", ""environment"", ""createTime"", ""isDelay"", ""delayAt"", ""expireTime"", ""eventName"", ""eventHandlerName"", ""eventBody"", ""eventItems"", ""retryCount"", ""status"", ""isLocking"", ""lockEnd"")
VALUES(@msgId, @environment, @createTime, @isDelay, @delayAt, @expireTime, @eventName, @eventHandlerName, @eventBody, @eventItems, @retryCount, @status, @isLocking, @lockEnd) RETURNING id;
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

            var id = await SqlScalar(sql, parameters, cancellationToken).ConfigureAwait(false);
            if (id is null || !id.TryParse<long>(out var longId))
                throw new DbUpdateException();

            message.Id = longId;
            return longId;
        }

        public async Task UpdatePublished(long id, string status, int retryCount, DateTimeOffset? expireTime,
            CancellationToken cancellationToken = default)
        {
            var sql = $@"
UPDATE {Options.CurrentValue.FullPublishedTableName}
SET ""status"" = '{status}', ""retryCount"" = {retryCount}, ""expireTime"" = {expireTime?.GetLongDate() ?? 0}
WHERE ""id"" = {id}
;
";

            await NonQuery(sql, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateReceived(long id, string status, int retryCount,
            DateTimeOffset? expireTime,
            CancellationToken cancellationToken = default)
        {
            var sql = $@"
UPDATE {Options.CurrentValue.FullReceivedTableName}
SET ""status"" = '{status}', ""retryCount"" = {retryCount}, ""expireTime"" = {expireTime?.GetLongDate() ?? 0}
WHERE ""id"" = {id}
;
";
            await NonQuery(sql, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> TryLockReceived(long id, DateTimeOffset lockEndAt,
            CancellationToken cancellationToken)
        {
            if (lockEndAt <= DateTimeOffset.Now)
                throw new ArgumentOutOfRangeException(nameof(lockEndAt));
            var nowLong = DateTime.Now.GetLongDate();

            var sql = $@"
UPDATE {Options.CurrentValue.FullReceivedTableName}
SET ""isLocking"" = true, ""lockEnd"" = {lockEndAt.GetLongDate()}
WHERE ""id"" = {id} AND (""isLocking"" = false OR ""lockEnd"" < {nowLong})
;
";
            try
            {
                return await NonQuery(sql, null, cancellationToken).ConfigureAwait(false) == 1;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"[EventBus-PostgreSql] TryLockReceived error.");
                return false;
            }
        }

        public async Task DeleteExpires(CancellationToken cancellationToken = default)
        {
            var now = DateTime.Now.GetLongDate();
            var sql = $@"
DELETE FROM {Options.CurrentValue.FullPublishedTableName} WHERE ""expireTime"" > 0 AND ""expireTime"" < {now} AND ""status"" != '{MessageStatus.Scheduled}';
DELETE FROM {Options.CurrentValue.FullReceivedTableName} WHERE ""expireTime"" > 0 AND ""expireTime"" < {now} AND ""status"" != '{MessageStatus.Scheduled}';
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
SELECT * FROM {Options.CurrentValue.FullPublishedTableName}
WHERE
    ""environment"" = '{environment}'
    AND ""createTime"" < {createTimeLimit}
    AND ""retryCount"" < {maxFailedRetryCount}
    AND (""isLocking"" = false OR ""lockEnd"" < {nowLong})
    AND (""status"" = '{MessageStatus.Scheduled}' OR ""status"" = '{MessageStatus.Failed}')
LIMIT {count};
";

            var table = await SqlQuery(sql, null, cancellationToken).ConfigureAwait(false);
            if (table.Rows.Count == 0) return new List<MessageStorageModel>();
            var idsBuilder = new StringBuilder();
            var list = table.AsEnumerable()
                .Select(row =>
                {
                    var id = row.GetValue<long>("id");
                    idsBuilder.Append(id.ToString());
                    idsBuilder.Append(",");

                    return RowToPublishedModel(row);
                }).ToList();
            var ids = idsBuilder.ToString();
            ids = ids.TrimEnd(',');

            var lockEnd = now.AddSeconds(lockSecond).GetLongDate();
            var updateSql = $@"
UPDATE {Options.CurrentValue.FullPublishedTableName}
SET ""isLocking"" = true, ""lockEnd"" = {lockEnd}
WHERE ""id"" IN ({ids}) AND (""isLocking"" = false OR ""lockEnd"" < {nowLong});
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
SELECT * FROM {Options.CurrentValue.FullReceivedTableName}
WHERE
    ""environment"" = '{environment}'
    AND ((""isDelay"" = false AND ""createTime"" < {createTimeLimit}) OR (""isDelay"" = true AND ""delayAt"" <= {nowLong} ))
    AND ""retryCount"" < {maxFailedRetryCount}
    AND (""isLocking"" = false OR ""lockEnd"" < {nowLong})
    AND (""status"" = '{MessageStatus.Scheduled}' OR ""status"" = '{MessageStatus.Failed}')
LIMIT {count};
";

            var table = await SqlQuery(sql, null, cancellationToken).ConfigureAwait(false);
            if (table.Rows.Count == 0) return new List<MessageStorageModel>();
            var idsBuilder = new StringBuilder();
            var list = table.AsEnumerable()
                .Select(row =>
                {
                    var id = row.GetValue<long>("id");
                    idsBuilder.Append(id.ToString());
                    idsBuilder.Append(",");

                    return RowToReceivedModel(row);
                }).ToList();
            var ids = idsBuilder.ToString();
            ids = ids.TrimEnd(',');

            var lockEnd = now.AddSeconds(lockSecond).GetLongDate();
            var updateSql = $@"
UPDATE {Options.CurrentValue.FullReceivedTableName}
SET ""isLocking"" = true, ""lockEnd"" = {lockEnd}
WHERE ""id"" IN ({ids}) AND (""isLocking"" = false OR ""lockEnd"" < {nowLong});
";
            var rows = await NonQuery(updateSql, null, cancellationToken).ConfigureAwait(false);
            return rows != list.Count ? new List<MessageStorageModel>() : list;
        }

        private async Task<DataTable> SqlQuery(string sql, NpgsqlParameter[]? parameters = null, CancellationToken cancellationToken = default)
        {
            await using var connection = new NpgsqlConnection(ConnectionString.ConnectionString);
            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync(cancellationToken);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            if (!parameters.IsNullOrEmpty())
                foreach (var mySqlParameter in parameters!)
                    cmd.Parameters.Add(mySqlParameter);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var table = new DataTable();
            table.Load(reader);
            return table;
        }

        private async Task<object?> SqlScalar(string sql, CancellationToken cancellationToken = default)
        {
            await using var connection = new NpgsqlConnection(ConnectionString.ConnectionString);
            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync(cancellationToken);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            return await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<object?> SqlScalar(string sql, NpgsqlParameter[]? parameter,
            CancellationToken cancellationToken = default)
        {
            await using var connection = new NpgsqlConnection(ConnectionString.ConnectionString);
            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync(cancellationToken);
            await using var cmd = connection.CreateCommand();
            if (!parameter.IsNullOrEmpty())
                foreach (var mySqlParameter in parameter!)
                    cmd.Parameters.Add(mySqlParameter);
            cmd.CommandText = sql;
            return await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<int> NonQuery(string sql, NpgsqlParameter[]? parameter,
            CancellationToken cancellationToken = default)
        {
            await using var connection = new NpgsqlConnection(ConnectionString.ConnectionString);
            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync(cancellationToken);
            await using var cmd = connection.CreateCommand();
            if (!parameter.IsNullOrEmpty())
                foreach (var mySqlParameter in parameter!)
                    cmd.Parameters.Add(mySqlParameter);
            cmd.CommandText = sql;
            return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<object?> SqlScalar(ITransactionContext? transactionContext, string sql, NpgsqlParameter[] parameter,
            CancellationToken cancellationToken = default)
        {
            if (transactionContext is null)
                return await SqlScalar(sql, parameter, cancellationToken).ConfigureAwait(false);

            if (!(transactionContext is RelationDbStorageTransactionContext relationDbStorageTransactionContext))
                throw new InvalidCastException(
                    $"[EventBus-PostgreSql]Storage only support transaction context of {typeof(RelationDbStorageTransactionContext)}");

            if (relationDbStorageTransactionContext.DbTransaction is NpgsqlTransaction tran)
            {
                var connection = tran.Connection;
                await using var cmd = connection!.CreateCommand();
                if (!parameter.IsNullOrEmpty())
                    foreach (var mySqlParameter in parameter)
                        cmd.Parameters.Add(mySqlParameter);

                cmd.CommandText = sql;
                cmd.Transaction = tran;

                return await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }
            else
                throw new InvalidCastException("[EventBus-PostgreSql]Invalid mysql connection instance");
        }

        private MessageStorageModel RowToPublishedModel(DataRow row)
        {
            return new MessageStorageModel
            {
                Id = row.GetValue<long>("id"),
                MsgId = row.GetValue<string>("msgId"),
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
        }

        private MessageStorageModel RowToReceivedModel(DataRow row)
        {
            return new MessageStorageModel
            {
                Id = row.GetValue<long>("id"),
                MsgId = row.GetValue<string>("msgId"),
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
        }
    }
}