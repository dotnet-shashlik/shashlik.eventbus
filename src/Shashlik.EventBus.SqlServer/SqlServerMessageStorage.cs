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
using Shashlik.Utils.Extensions;

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

        public async ValueTask<bool> ExistsPublishMessage(string msgId, CancellationToken cancellationToken = default)
        {
            var sql = $@"
SELECT COUNT([msgId]) FROM [{Options.CurrentValue.PublishTableName}] WHERE [msgId]='{msgId}';";

            var count = (await SqlScalar(sql, cancellationToken).ConfigureAwait(false))?.ParseTo<int>() ?? 0;
            return count > 0;
        }

        public async ValueTask<bool> ExistsReceiveMessage(string msgId, CancellationToken cancellationToken = default)
        {
            var sql = $@"
SELECT COUNT([msgId]) FROM [{Options.CurrentValue.ReceiveTableName}] WHERE [msgId]='{msgId}';";

            var count = (await SqlScalar(sql, cancellationToken).ConfigureAwait(false))?.ParseTo<int>() ?? 0;
            return count > 0;
        }

        public async Task<MessageStorageModel?> FindPublishedById(string id,
            CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM [{Options.CurrentValue.PublishTableName}] WHERE [msgId]='{id}';";

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

        public async Task<MessageStorageModel?> FindReceivedById(string id,
            CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT * FROM [{Options.CurrentValue.ReceiveTableName}] WHERE [msgId]='{id}';";

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

        public async Task SavePublished(MessageStorageModel message, ITransactionContext? transactionContext,
            CancellationToken cancellationToken = default)
        {
            var sql = $@"
INSERT INTO [{Options.CurrentValue.PublishTableName}]
([msgId], [environment], [createTime], [delayAt], [expireTime], [eventName], [eventBody], [eventItems], [retryCount], [status], [isLocking], [lockEnd])
VALUES(@msgId, @environment, @createTime, @delayAt, @expireTime, @eventName, @eventBody, @eventItems, @retryCount, @status, @isLocking, @lockEnd);
";

            var parameters = new[]
            {
                new SqlParameter("@msgId", SqlDbType.VarChar) {Value = message.MsgId},
                new SqlParameter("@environment", SqlDbType.VarChar) {Value = message.Environment},
                new SqlParameter("@createTime", SqlDbType.BigInt) {Value = message.CreateTime.GetLongDate()},
                new SqlParameter("@delayAt", SqlDbType.BigInt) {Value = message.DelayAt?.GetLongDate() ?? 0},
                new SqlParameter("@expireTime", SqlDbType.BigInt) {Value = message.ExpireTime?.GetLongDate() ?? 0},
                new SqlParameter("@eventName", SqlDbType.VarChar) {Value = message.EventName},
                new SqlParameter("@eventBody", SqlDbType.Text) {Value = message.EventBody},
                new SqlParameter("@eventItems", SqlDbType.Text) {Value = message.EventItems},
                new SqlParameter("@retryCount", SqlDbType.Int) {Value = message.RetryCount},
                new SqlParameter("@status", SqlDbType.VarChar) {Value = message.Status},
                new SqlParameter("@isLocking", SqlDbType.Bit) {Value = message.IsLocking ? 1 : 0},
                new SqlParameter("@lockEnd", SqlDbType.BigInt) {Value = message.LockEnd?.GetLongDate() ?? 0},
            };

            var row = await NonQuery(transactionContext, sql, parameters, cancellationToken).ConfigureAwait(false);
            if (row == 0)
                throw new DbUpdateException();
        }

        public async Task SaveReceived(MessageStorageModel message, CancellationToken cancellationToken = default)
        {
            var sql = $@"
INSERT INTO [{Options.CurrentValue.ReceiveTableName}]
([msgId], [environment], [createTime], [isDelay], [delayAt], [expireTime], [eventName], [eventHandlerName], [eventBody], [eventItems], [retryCount], [status], [isLocking], [lockEnd])
VALUES(@msgId, @environment, @createTime, @isDelay, @delayAt, @expireTime, @eventName, @eventHandlerName, @eventBody, @eventItems, @retryCount, @status, @isLocking, @lockEnd);
";

            var parameters = new[]
            {
                new SqlParameter("@msgId", SqlDbType.VarChar) {Value = message.MsgId},
                new SqlParameter("@environment", SqlDbType.VarChar) {Value = message.Environment},
                new SqlParameter("@createTime", SqlDbType.BigInt) {Value = message.CreateTime.GetLongDate()},
                new SqlParameter("@isDelay", SqlDbType.Bit) {Value = message.DelayAt.HasValue ? 1 : 0},
                new SqlParameter("@delayAt", SqlDbType.BigInt) {Value = message.DelayAt?.GetLongDate() ?? 0},
                new SqlParameter("@expireTime", SqlDbType.BigInt) {Value = message.ExpireTime?.GetLongDate() ?? 0},
                new SqlParameter("@eventName", SqlDbType.VarChar) {Value = message.EventName},
                new SqlParameter("@eventHandlerName", SqlDbType.VarChar) {Value = message.EventHandlerName},
                new SqlParameter("@eventBody", SqlDbType.Text) {Value = message.EventBody},
                new SqlParameter("@eventItems", SqlDbType.Text) {Value = message.EventItems},
                new SqlParameter("@retryCount", SqlDbType.Int) {Value = message.RetryCount},
                new SqlParameter("@status", SqlDbType.VarChar) {Value = message.Status},
                new SqlParameter("@isLocking", SqlDbType.Bit) {Value = message.IsLocking ? 1 : 0},
                new SqlParameter("@lockEnd", SqlDbType.BigInt) {Value = message.LockEnd?.GetLongDate() ?? 0}
            };

            var row = await NonQuery(sql, parameters, cancellationToken).ConfigureAwait(false);
            if (row == 0)
                throw new DbUpdateException();
        }

        public async Task UpdatePublished(string msgId, string status, int retryCount, DateTimeOffset? expireTime,
            CancellationToken cancellationToken = default)
        {
            var sql = $@"
UPDATE [{Options.CurrentValue.PublishTableName}]
SET [status] = '{status}', [retryCount] = {retryCount}, [expireTime] = {expireTime?.GetLongDate() ?? 0}
WHERE [msgId] = '{msgId}'
";

            await NonQuery(sql, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateReceived(string msgId, string status, int retryCount, DateTimeOffset? expireTime,
            CancellationToken cancellationToken = default)
        {
            var sql = $@"
UPDATE [{Options.CurrentValue.ReceiveTableName}]
SET [status] = '{status}', [retryCount] = {retryCount}, [expireTime] = {expireTime?.GetLongDate() ?? 0}
WHERE [msgId] = '{msgId}'
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
UPDATE [{Options.CurrentValue.ReceiveTableName}]
SET [isLocking] = 1, [lockEnd] = {lockEndAt.GetLongDate()}
WHERE [msgId] = '{msgId}' AND ([isLocking] = 0 OR [lockEnd] < {nowLong})
";
            return await NonQuery(sql, null, cancellationToken).ConfigureAwait(false) == 1;
        }

        public async Task DeleteExpires(CancellationToken cancellationToken = default)
        {
            var now = DateTime.Now.GetLongDate();
            var sql = $@"
DELETE FROM [{Options.CurrentValue.PublishTableName}] WHERE [expireTime] != 0 AND [expireTime] < {now} AND [status] != '{MessageStatus.Scheduled}';
DELETE FROM [{Options.CurrentValue.ReceiveTableName}] WHERE [expireTime] != 0 AND [expireTime] < {now} AND [status] != '{MessageStatus.Scheduled}';
";
            await NonQuery(sql, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task<List<MessageStorageModel>> GetPublishedMessagesOfNeedRetryAndLock(
            int count,
            int delayRetrySecond,
            int maxFailedRetryCount,
            string environment,
            int lockSecond,
            CancellationToken cancellationToken = default)
        {
            var createTimeLimit = DateTime.Now.AddSeconds(-delayRetrySecond).GetLongDate();
            var now = DateTime.Now;
            var nowLong = now.GetLongDate();

            var sql = $@"
SELECT * FROM [{Options.CurrentValue.PublishTableName}]
WHERE
    [environment] = '{environment}'
    AND [createTime] < {createTimeLimit}
    AND [retryCount] < {maxFailedRetryCount}
    AND ([isLocking] = 0 OR [lockEnd] < {nowLong})
    AND ([status] = '{MessageStatus.Scheduled}' OR [status] = '{MessageStatus.Failed}')
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
UPDATE [{Options.CurrentValue.PublishTableName}]
SET [isLocking] = 1, [lockEnd] = {lockEnd}
WHERE [msgId] IN ({ids}) AND ([isLocking] = 0 OR [lockEnd] < {nowLong});
";
            var rows = await NonQuery(updateSql, null, cancellationToken).ConfigureAwait(false);
            return rows != list.Count ? new List<MessageStorageModel>() : list;
        }

        public async Task<List<MessageStorageModel>> GetReceivedMessagesOfNeedRetryAndLock(
            int count,
            int delayRetrySecond,
            int maxFailedRetryCount,
            string environment,
            int lockSecond,
            CancellationToken cancellationToken = default)
        {
            var createTimeLimit = DateTime.Now.AddSeconds(-delayRetrySecond).GetLongDate();
            var now = DateTime.Now;
            var nowLong = now.GetLongDate();

            var sql = $@"
SELECT * FROM [{Options.CurrentValue.ReceiveTableName}]
WHERE
    [environment] = '{environment}'
    AND (([isDelay] = 0 AND [createTime] < {createTimeLimit}) OR ([isDelay] = 1 AND [delayAt] <= {nowLong} ))
    AND [retryCount] < {maxFailedRetryCount}
    AND ([isLocking] = 0 OR [lockEnd] < {nowLong})
    AND ([status] = '{MessageStatus.Scheduled}' OR [status] = '{MessageStatus.Failed}')
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
UPDATE [{Options.CurrentValue.ReceiveTableName}]
SET [isLocking] = 1, [lockEnd] = {lockEnd}
WHERE [msgId] IN ({ids}) AND ([isLocking] = 0 OR [lockEnd] < {nowLong});
";
            var rows = await NonQuery(updateSql, null, cancellationToken).ConfigureAwait(false);
            return rows != list.Count ? new List<MessageStorageModel>() : list;
        }

        private async Task<DataTable> SqlQuery(string sql, CancellationToken cancellationToken = default)
        {
            await using var connection = new SqlConnection(ConnectionString.ConnectionString);
            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync(cancellationToken);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var table = new DataTable();
            table.Load(reader);
            return table;
        }

        private async Task<object> SqlScalar(string sql, CancellationToken cancellationToken = default)
        {
            await using var connection = new SqlConnection(ConnectionString.ConnectionString);
            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync(cancellationToken);
            await using var cmd = connection.CreateCommand();
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
                foreach (var sqlParameter in parameter!)
                    cmd.Parameters.Add(sqlParameter);
            cmd.CommandText = sql;
            return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<int> NonQuery(ITransactionContext? transactionContext, string sql, SqlParameter[] parameter,
            CancellationToken cancellationToken = default)
        {
            if (transactionContext is null)
                return await NonQuery(sql, parameter, cancellationToken).ConfigureAwait(false);

            if (!(transactionContext is RelationDbStorageTransactionContext relationDbStorageTransactionContext))
                throw new InvalidCastException(
                    $"Event bus mysql storage only support transaction context of {typeof(RelationDbStorageTransactionContext)}");

            if (relationDbStorageTransactionContext.DbTransaction is SqlTransaction tran)
            {
                var connection = tran.Connection;
                await using var cmd = connection.CreateCommand();
                if (!parameter.IsNullOrEmpty())
                    foreach (var mySqlParameter in parameter)
                        cmd.Parameters.Add(mySqlParameter);

                cmd.CommandText = sql;
                cmd.Transaction = tran;

                return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            else
                throw new InvalidCastException("Invalid mysql connection instance");
        }
    }
}