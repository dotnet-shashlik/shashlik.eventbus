using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;
using Shashlik.EventBus.RelationDbStorage;
using Shashlik.EventBus.Utils;

// ReSharper disable ConvertIfStatementToSwitchExpression

// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable RedundantIfElseBlock

namespace Shashlik.EventBus.SqlServer
{
    public class SqlServerMessageStorage : IMessageStorage
    {
        public SqlServerMessageStorage(IOptionsMonitor<EventBusSqlServerOptions> options, IConnectionString connectionString)
        {
            Options = options;
            ConnectionString = connectionString;
        }

        private IOptionsMonitor<EventBusSqlServerOptions> Options { get; }
        private IConnectionString ConnectionString { get; }

        public async ValueTask<bool> IsCommittedAsync(string msgId, CancellationToken cancellationToken = default)
        {
            var sql = $@"
SELECT TOP 1 1 FROM {Options.CurrentValue.FullPublishedTableName} WHERE [msgId]='{msgId}';";

            var count = (await SqlScalar(sql, cancellationToken).ConfigureAwait(false))?.ParseTo<int>() ?? 0;
            return count > 0;
        }

        public async Task<MessageStorageModel?> FindPublishedByMsgIdAsync(string msgId,
            CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM {Options.CurrentValue.FullPublishedTableName} WHERE [msgId]='{msgId}';";

            var table = await SqlQuery(sql, null, cancellationToken).ConfigureAwait(false);
            if (table.Rows.Count == 0)
                return null;

            return RowToPublishedModel(table.Rows[0]);
        }

        public async Task<MessageStorageModel?> FindPublishedByIdAsync(long id, CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM {Options.CurrentValue.FullPublishedTableName} WHERE [id]={id};";

            var table = await SqlQuery(sql, null, cancellationToken).ConfigureAwait(false);
            if (table.Rows.Count == 0)
                return null;
            return RowToPublishedModel(table.Rows[0]);
        }

        public async Task<MessageStorageModel?> FindReceivedByMsgIdAsync(string msgId, EventHandlerDescriptor eventHandlerDescriptor,
            CancellationToken cancellationToken = default)
        {
            var sql =
                $"SELECT * FROM {Options.CurrentValue.FullReceivedTableName} WHERE [msgId]='{msgId}' AND [eventHandlerName]='{eventHandlerDescriptor.EventHandlerName}';";

            var table = await SqlQuery(sql, null, cancellationToken).ConfigureAwait(false);
            if (table.Rows.Count == 0)
                return null;

            return RowToReceivedModel(table.Rows[0]);
        }

        public async Task<MessageStorageModel?> FindReceivedByIdAsync(long id, CancellationToken cancellationToken)
        {
            var sql =
                $"SELECT * FROM {Options.CurrentValue.FullReceivedTableName} WHERE [id]={id};";

            var table = await SqlQuery(sql, null, cancellationToken).ConfigureAwait(false);
            if (table.Rows.Count == 0)
                return null;

            return RowToReceivedModel(table.Rows[0]);
        }

        public async Task<List<MessageStorageModel>> SearchPublishedAsync(string? eventName, string? status, int skip, int take,
            CancellationToken cancellationToken)
        {
            var where = new StringBuilder();
            if (!eventName.IsNullOrWhiteSpace())
                where.Append(" AND [eventName]=@eventName");
            if (!status.IsNullOrWhiteSpace())
                where.Append(" AND [status]=@status");

            var sql = $@"
SELECT * FROM 
(
    SELECT *,ROW_NUMBER() OVER(ORDER BY [createTime] DESC) AS rowNumber FROM {Options.CurrentValue.FullPublishedTableName}
    WHERE 
        1=1{where}
) AS A
WHERE A.rowNumber BETWEEN {skip} and {skip + take}
ORDER BY A.createTime DESC;
";

            var parameters = new[]
            {
                new SqlParameter("@eventName", SqlDbType.VarChar) { Value = eventName },
                new SqlParameter("@status", SqlDbType.VarChar) { Value = status },
            };

            var table = await SqlQuery(sql, parameters, cancellationToken).ConfigureAwait(false);
            if (table.Rows.Count == 0)
                return new List<MessageStorageModel>();
            return table.AsEnumerable()
                .Select(RowToPublishedModel)
                .ToList();
        }

        public async Task<List<MessageStorageModel>> SearchReceived(string? eventName, string? eventHandlerName, string? status, int skip,
            int take,
            CancellationToken cancellationToken)
        {
            var where = new StringBuilder();
            if (!eventName.IsNullOrWhiteSpace())
                where.Append(" AND [eventName]=@eventName");
            if (!eventHandlerName.IsNullOrWhiteSpace())
                where.Append(" AND [eventHandlerName]=@eventHandlerName");
            if (!status.IsNullOrWhiteSpace())
                where.Append(" AND [status]=@status");

            var sql = $@"
SELECT * FROM 
(
    SELECT *,ROW_NUMBER() OVER(ORDER BY [createTime] DESC) AS rowNumber FROM {Options.CurrentValue.FullReceivedTableName}
    WHERE 
        1=1{where}
) AS A
WHERE A.rowNumber BETWEEN {skip} and {skip + take}
ORDER BY A.createTime DESC;
";
            var parameters = new[]
            {
                new SqlParameter("@eventName", SqlDbType.VarChar) { Value = eventName },
                new SqlParameter("@eventHandlerName", SqlDbType.VarChar) { Value = eventHandlerName },
                new SqlParameter("@status", SqlDbType.VarChar) { Value = status },
            };

            var table = await SqlQuery(sql, parameters, cancellationToken).ConfigureAwait(false);
            if (table.Rows.Count == 0)
                return new List<MessageStorageModel>();
            return table.AsEnumerable()
                .Select(RowToPublishedModel)
                .ToList();
        }

        public async Task<long> SavePublishedAsync(MessageStorageModel message, ITransactionContext? transactionContext,
            CancellationToken cancellationToken = default)
        {
            var sql = $@"
INSERT INTO {Options.CurrentValue.FullPublishedTableName}
([msgId], [environment], [createTime], [delayAt], [expireTime], [eventName], [eventBody], [eventItems], [retryCount], [status], [isLocking], [lockEnd])
VALUES(@msgId, @environment, @createTime, @delayAt, @expireTime, @eventName, @eventBody, @eventItems, @retryCount, @status, @isLocking, @lockEnd);
SELECT SCOPE_IDENTITY();
";

            var parameters = new[]
            {
                new SqlParameter("@msgId", SqlDbType.VarChar) { Value = message.MsgId },
                new SqlParameter("@environment", SqlDbType.VarChar) { Value = message.Environment },
                new SqlParameter("@createTime", SqlDbType.BigInt) { Value = message.CreateTime.GetLongDate() },
                new SqlParameter("@delayAt", SqlDbType.BigInt) { Value = message.DelayAt?.GetLongDate() ?? 0 },
                new SqlParameter("@expireTime", SqlDbType.BigInt) { Value = message.ExpireTime?.GetLongDate() ?? 0 },
                new SqlParameter("@eventName", SqlDbType.VarChar) { Value = message.EventName },
                new SqlParameter("@eventBody", SqlDbType.Text) { Value = message.EventBody },
                new SqlParameter("@eventItems", SqlDbType.Text) { Value = message.EventItems },
                new SqlParameter("@retryCount", SqlDbType.Int) { Value = message.RetryCount },
                new SqlParameter("@status", SqlDbType.VarChar) { Value = message.Status },
                new SqlParameter("@isLocking", SqlDbType.Bit) { Value = message.IsLocking ? 1 : 0 },
                new SqlParameter("@lockEnd", SqlDbType.BigInt) { Value = message.LockEnd?.GetLongDate() ?? 0 },
            };

            var id = await SqlScalar(transactionContext, sql, parameters, cancellationToken).ConfigureAwait(false);
            if (id is null || !id.TryParse<long>(out var longId))
                throw new DbUpdateException();

            message.Id = longId;
            return longId;
        }

        public async Task<long> SaveReceivedAsync(MessageStorageModel message, CancellationToken cancellationToken = default)
        {
            var sql = $@"
INSERT INTO {Options.CurrentValue.FullReceivedTableName}
([msgId], [environment], [createTime], [isDelay], [delayAt], [expireTime], [eventName], [eventHandlerName], [eventBody], [eventItems], [retryCount], [status], [isLocking], [lockEnd])
VALUES(@msgId, @environment, @createTime, @isDelay, @delayAt, @expireTime, @eventName, @eventHandlerName, @eventBody, @eventItems, @retryCount, @status, @isLocking, @lockEnd);
SELECT SCOPE_IDENTITY();
";

            var parameters = new[]
            {
                new SqlParameter("@msgId", SqlDbType.VarChar) { Value = message.MsgId },
                new SqlParameter("@environment", SqlDbType.VarChar) { Value = message.Environment },
                new SqlParameter("@createTime", SqlDbType.BigInt) { Value = message.CreateTime.GetLongDate() },
                new SqlParameter("@isDelay", SqlDbType.Bit) { Value = message.DelayAt.HasValue ? 1 : 0 },
                new SqlParameter("@delayAt", SqlDbType.BigInt) { Value = message.DelayAt?.GetLongDate() ?? 0 },
                new SqlParameter("@expireTime", SqlDbType.BigInt) { Value = message.ExpireTime?.GetLongDate() ?? 0 },
                new SqlParameter("@eventName", SqlDbType.VarChar) { Value = message.EventName },
                new SqlParameter("@eventHandlerName", SqlDbType.VarChar) { Value = message.EventHandlerName },
                new SqlParameter("@eventBody", SqlDbType.Text) { Value = message.EventBody },
                new SqlParameter("@eventItems", SqlDbType.Text) { Value = message.EventItems },
                new SqlParameter("@retryCount", SqlDbType.Int) { Value = message.RetryCount },
                new SqlParameter("@status", SqlDbType.VarChar) { Value = message.Status },
                new SqlParameter("@isLocking", SqlDbType.Bit) { Value = message.IsLocking ? 1 : 0 },
                new SqlParameter("@lockEnd", SqlDbType.BigInt) { Value = message.LockEnd?.GetLongDate() ?? 0 }
            };

            var id = await SqlScalar(sql, parameters, cancellationToken).ConfigureAwait(false);
            if (id is null || !id.TryParse<long>(out var longId))
                throw new DbUpdateException();

            message.Id = longId;
            return longId;
        }

        public async Task UpdatePublishedAsync(long id, string status, int retryCount, DateTimeOffset? expireTime,
            CancellationToken cancellationToken = default)
        {
            var sql = $@"
UPDATE {Options.CurrentValue.FullPublishedTableName}
SET [status] = '{status}', [retryCount] = {retryCount}, [expireTime] = {expireTime?.GetLongDate() ?? 0}
WHERE [id] = {id};
";

            await NonQuery(sql, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateReceivedAsync(long id, string status, int retryCount,
            DateTimeOffset? expireTime,
            CancellationToken cancellationToken = default)
        {
            var sql = $@"
UPDATE {Options.CurrentValue.FullReceivedTableName}
SET [status] = '{status}', [retryCount] = {retryCount}, [expireTime] = {expireTime?.GetLongDate() ?? 0}
WHERE [id] = {id};
";
            await NonQuery(sql, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> TryLockPublishedAsync(long id, DateTimeOffset lockEndAt, CancellationToken cancellationToken)
        {
            if (lockEndAt <= DateTimeOffset.Now)
                throw new ArgumentOutOfRangeException(nameof(lockEndAt));
            var nowLong = DateTime.Now.GetLongDate();

            var sql = $@"
            UPDATE {Options.CurrentValue.FullPublishedTableName}
            SET [isLocking] = 1, [lockEnd] = {lockEndAt.GetLongDate()}
            WHERE [id] = {id} AND ([isLocking] = 0 OR [lockEnd] < {nowLong});
            ";
            return await NonQuery(sql, null, cancellationToken).ConfigureAwait(false) == 1;
        }

        public async Task<bool> TryLockReceivedAsync(long id, DateTimeOffset lockEndAt, CancellationToken cancellationToken)
        {
            if (lockEndAt <= DateTimeOffset.Now)
                throw new ArgumentOutOfRangeException(nameof(lockEndAt));
            var nowLong = DateTime.Now.GetLongDate();

            var sql = $@"
UPDATE {Options.CurrentValue.FullReceivedTableName}
SET [isLocking] = 1, [lockEnd] = {lockEndAt.GetLongDate()}
WHERE [id] = {id} AND ([isLocking] = 0 OR [lockEnd] < {nowLong});
";
            return await NonQuery(sql, null, cancellationToken).ConfigureAwait(false) == 1;
        }

        public async Task DeleteExpiresAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.Now.GetLongDate();
            var sql = $@"
DELETE FROM {Options.CurrentValue.FullPublishedTableName} WHERE [expireTime] > 0 AND [expireTime] < {now} AND [status] = '{MessageStatus.Succeeded}';
DELETE FROM {Options.CurrentValue.FullReceivedTableName} WHERE [expireTime] > 0 AND [expireTime] < {now} AND [status] = '{MessageStatus.Succeeded}';
";
            await NonQuery(sql, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task<List<MessageStorageModel>> GetPublishedMessagesOfNeedRetryAsync(
            int count,
            int delayRetrySecond,
            int maxFailedRetryCount,
            string environment,
            CancellationToken cancellationToken = default)
        {
            var createTimeLimit = DateTime.Now.AddSeconds(-delayRetrySecond).GetLongDate();
            var now = DateTime.Now;
            var nowLong = now.GetLongDate();

            var sql = $@"
SELECT TOP {count} * FROM {Options.CurrentValue.FullPublishedTableName}
WHERE
    [environment] = '{environment}'
    AND [createTime] < {createTimeLimit}
    AND [retryCount] < {maxFailedRetryCount}
    AND ([isLocking] = 0 OR [lockEnd] < {nowLong})
    AND ([status] = '{MessageStatus.Scheduled}' OR [status] = '{MessageStatus.Failed}')
;
";

            var table = await SqlQuery(sql, null, cancellationToken).ConfigureAwait(false);
            if (table.Rows.Count == 0) return new List<MessageStorageModel>();
            return table.AsEnumerable()
                .Select(RowToPublishedModel).ToList();
        }

        public async Task<List<MessageStorageModel>> GetReceivedMessagesOfNeedRetryAsync(
            int count,
            int delayRetrySecond,
            int maxFailedRetryCount,
            string environment,
            CancellationToken cancellationToken = default)
        {
            var createTimeLimit = DateTime.Now.AddSeconds(-delayRetrySecond).GetLongDate();
            var now = DateTime.Now;
            var nowLong = now.GetLongDate();

            var sql = $@"
SELECT TOP {count} * FROM {Options.CurrentValue.FullReceivedTableName}
WHERE
    [environment] = '{environment}'
    AND (([isDelay] = 0 AND [createTime] < {createTimeLimit}) OR ([isDelay] = 1 AND [delayAt] <= {nowLong} ))
    AND [retryCount] < {maxFailedRetryCount}
    AND ([isLocking] = 0 OR [lockEnd] < {nowLong})
    AND ([status] = '{MessageStatus.Scheduled}' OR [status] = '{MessageStatus.Failed}')
;
";

            var table = await SqlQuery(sql, null, cancellationToken).ConfigureAwait(false);
            if (table.Rows.Count == 0) return new List<MessageStorageModel>();
            return table.AsEnumerable()
                .Select(RowToReceivedModel).ToList();
        }

        private async Task<DataTable> SqlQuery(string sql, SqlParameter[]? parameters = null, CancellationToken cancellationToken = default)
        {
            await using var connection = new SqlConnection(ConnectionString.ConnectionString);
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
            await using var connection = new SqlConnection(ConnectionString.ConnectionString);
            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync(cancellationToken);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            return await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<object?> SqlScalar(string sql, SqlParameter[]? parameter,
            CancellationToken cancellationToken = default)
        {
            await using var connection = new SqlConnection(ConnectionString.ConnectionString);
            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync(cancellationToken);
            await using var cmd = connection.CreateCommand();
            if (!parameter.IsNullOrEmpty())
                foreach (var mySqlParameter in parameter!)
                    cmd.Parameters.Add(mySqlParameter);
            cmd.CommandText = sql;
            return await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<int> NonQuery(string sql, SqlParameter[]? parameter,
            CancellationToken cancellationToken = default)
        {
            await using var connection = new SqlConnection(ConnectionString.ConnectionString);
            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync(cancellationToken);
            await using var cmd = connection.CreateCommand();
            if (!parameter.IsNullOrEmpty())
                foreach (var mySqlParameter in parameter!)
                    cmd.Parameters.Add(mySqlParameter);
            cmd.CommandText = sql;
            return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<object?> SqlScalar(ITransactionContext? transactionContext, string sql, SqlParameter[] parameter,
            CancellationToken cancellationToken = default)
        {
            if (transactionContext is null || transactionContext is XaTransactionContext)
                return await SqlScalar(sql, parameter, cancellationToken).ConfigureAwait(false);

            if (!(transactionContext is RelationDbStorageTransactionContext relationDbStorageTransactionContext))
                throw new InvalidCastException(
                    $"[EventBus-SqlServer]Storage only support transaction context of {typeof(RelationDbStorageTransactionContext)}");

            if (relationDbStorageTransactionContext.DbTransaction is SqlTransaction tran)
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
                throw new InvalidCastException("[EventBus-SqlServer]Invalid mysql connection instance");
        }

        private MessageStorageModel RowToPublishedModel(DataRow row)
        {
            return new MessageStorageModel
            {
                Id = row.GetRowValue<long>("id"),
                MsgId = row.GetRowValue<string>("msgId"),
                Environment = row.GetRowValue<string>("environment"),
                CreateTime = row.GetRowValue<long>("createTime").LongToDateTimeOffset(),
                DelayAt = row.GetRowValue<long?>("delayAt")?.LongToDateTimeOffset(),
                ExpireTime = row.GetRowValue<long?>("expireTime")?.LongToDateTimeOffset(),
                EventName = row.GetRowValue<string>("eventName"),
                EventBody = row.GetRowValue<string>("eventBody"),
                EventItems = row.GetRowValue<string>("eventItems"),
                RetryCount = row.GetRowValue<int>("retryCount"),
                Status = row.GetRowValue<string>("status"),
                IsLocking = row.GetRowValue<bool>("isLocking"),
                LockEnd = row.GetRowValue<long?>("lockEnd")?.LongToDateTimeOffset()
            };
        }

        private MessageStorageModel RowToReceivedModel(DataRow row)
        {
            return new MessageStorageModel
            {
                Id = row.GetRowValue<long>("id"),
                MsgId = row.GetRowValue<string>("msgId"),
                Environment = row.GetRowValue<string>("environment"),
                CreateTime = row.GetRowValue<long>("createTime").LongToDateTimeOffset(),
                DelayAt = row.GetRowValue<long?>("delayAt")?.LongToDateTimeOffset(),
                ExpireTime = row.GetRowValue<long?>("expireTime")?.LongToDateTimeOffset(),
                EventName = row.GetRowValue<string>("eventName"),
                EventHandlerName = row.GetRowValue<string>("eventHandlerName"),
                EventBody = row.GetRowValue<string>("eventBody"),
                EventItems = row.GetRowValue<string>("eventItems"),
                RetryCount = row.GetRowValue<int>("retryCount"),
                Status = row.GetRowValue<string>("status"),
                IsLocking = row.GetRowValue<bool>("isLocking"),
                LockEnd = row.GetRowValue<long?>("lockEnd")?.LongToDateTimeOffset()
            };
        }
    }
}