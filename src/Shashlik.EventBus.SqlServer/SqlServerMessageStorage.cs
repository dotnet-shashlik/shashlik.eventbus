using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.RelationDbStorage;
using Shashlik.EventBus.Utils;

// ReSharper disable ConvertIfStatementToSwitchExpression
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable RedundantIfElseBlock

namespace Shashlik.EventBus.SqlServer
{
    public class SqlServerMessageStorage : IRelationDbStorage
    {
        public SqlServerMessageStorage(IOptionsMonitor<EventBusSqlServerOptions> options, IConnectionString connectionString)
        {
            Options = options;
            ConnectionString = connectionString;
        }

        private IOptionsMonitor<EventBusSqlServerOptions> Options { get; }
        private IConnectionString ConnectionString { get; }

        private IRelationDbStorage GetInvoker()
        {
            return this;
        }

        public async ValueTask<bool> IsCommittedAsync(string msgId, CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT TOP 1 1 FROM {Options.CurrentValue.FullPublishedTableName} WHERE msgId = @msgId;";
            var count = await GetInvoker().ScalarAsync<int>(sql, new { msgId }, cancellationToken);
            return count > 0;
        }

        public async Task<MessageStorageModel?> FindPublishedByMsgIdAsync(string msgId,
            CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM {Options.CurrentValue.FullPublishedTableName} WHERE msgId = @msgId;";
            return await GetInvoker().QueryOneModelAsync(sql, new { msgId }, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<MessageStorageModel?> FindPublishedByIdAsync(string storageId,
            CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM {Options.CurrentValue.FullPublishedTableName} WHERE id = @storageId;";
            return await GetInvoker().QueryOneModelAsync(sql, new { storageId }, cancellationToken);
        }

        public async Task<MessageStorageModel?> FindReceivedByMsgIdAsync(string msgId,
            EventHandlerDescriptor eventHandlerDescriptor,
            CancellationToken cancellationToken = default)
        {
            var sql =
                $"SELECT * FROM {Options.CurrentValue.FullReceivedTableName} WHERE msgId = @msgId AND eventHandlerName = @eventHandlerName;";

            return await GetInvoker().QueryOneModelAsync(sql,
                    new { msgId, eventHandlerName = eventHandlerDescriptor.EventHandlerName }, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<MessageStorageModel?> FindReceivedByIdAsync(string storageId,
            CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM {Options.CurrentValue.FullReceivedTableName} WHERE id = @storageId;";
            return await GetInvoker().QueryOneModelAsync(sql, new { storageId }, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<List<MessageStorageModel>> SearchPublishedAsync(string? eventName, string? status, int skip,
            int take,
            CancellationToken cancellationToken)
        {
            // var parameters = new List<SqliteParameter>();
            var where = new StringBuilder();
            if (!eventName.IsNullOrWhiteSpace())
            {
                where.Append(" AND eventName = @eventName");
                // parameters.Add(new SqliteParameter("@eventName", SqliteType.Text) { Value = eventName });
            }

            if (!status.IsNullOrWhiteSpace())
            {
                where.Append(" AND status = @status");
                // parameters.Add(new SqliteParameter("@status", SqliteType.Text) { Value = status });
            }

            var sql = $@"
SELECT * FROM 
(
    SELECT *, ROW_NUMBER() OVER(ORDER BY createTime DESC) AS rowNumber FROM {Options.CurrentValue.FullPublishedTableName}
    WHERE 
        1 = 1{where}
) AS A
WHERE A.rowNumber BETWEEN {skip} and {skip + take}
ORDER BY A.createTime DESC;
";

            return await GetInvoker().QueryModelAsync(sql, new { eventName, status }, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<List<MessageStorageModel>> SearchReceivedAsync(string? eventName, string? eventHandlerName,
            string? status, int skip,
            int take,
            CancellationToken cancellationToken)
        {
            // var parameters = new List<SqliteParameter>();
            var where = new StringBuilder();
            if (!eventName.IsNullOrWhiteSpace())
            {
                where.Append(" AND eventName = @eventName");
                // parameters.Add(new SqliteParameter("@eventName", SqliteType.Text) { Value = eventName });
            }

            if (!eventHandlerName.IsNullOrWhiteSpace())
            {
                where.Append(" AND eventHandlerName = @eventHandlerName");
                // parameters.Add(
                //     new SqliteParameter("@eventHandlerName", SqliteType.Text) { Value = eventHandlerName });
            }

            if (!status.IsNullOrWhiteSpace())
            {
                where.Append(" AND status = @status");
                // parameters.Add(new SqliteParameter("@status", SqliteType.Text) { Value = status });
            }

            var sql = $@"
SELECT * FROM 
(
    SELECT *,ROW_NUMBER() OVER(ORDER BY createTime DESC) AS rowNumber FROM {Options.CurrentValue.FullReceivedTableName}
    WHERE 
        1 = 1{where}
) AS A
WHERE A.rowNumber BETWEEN {skip} and {skip + take}
ORDER BY A.createTime DESC;
";

            return await GetInvoker()
                .QueryModelAsync(sql, new { eventName, eventHandlerName, status }, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<string> SavePublishedAsync(MessageStorageModel message,
            ITransactionContext? transactionContext,
            CancellationToken cancellationToken = default)
        {
            var sql = $@"
INSERT INTO {Options.CurrentValue.FullPublishedTableName}
(msgId, environment, createTime, delayAt, expireTime, eventName, eventBody, eventItems, retryCount, status, isLocking, lockEnd)
VALUES(@MsgId, @Environment, @CreateTime, @DelayAt, @ExpireTime, @EventName, @EventBody, @EventItems, @RetryCount, @Status, @IsLocking, @LockEnd);
SELECT SCOPE_IDENTITY();
";
            var id = await GetInvoker().ScalarAsync<int?>(transactionContext, sql, GetInvoker().ToSaveObject(message),
                    cancellationToken)
                .ConfigureAwait(false);
            if (id is null)
                throw new DbUpdateException();

            message.Id = id.ToString();
            return message.Id!;
        }

        public async Task<string> SaveReceivedAsync(MessageStorageModel message,
            CancellationToken cancellationToken = default)
        {
            var sql = $@"
INSERT INTO {Options.CurrentValue.FullReceivedTableName}
(msgId, environment, createTime, isDelay, delayAt, expireTime, eventName, eventHandlerName, eventBody, eventItems, retryCount, status, isLocking, lockEnd)
VALUES(@MsgId, @Environment, @CreateTime, @IsDelay, @DelayAt, @ExpireTime, @EventName, @EventHandlerName, @EventBody, @EventItems, @RetryCount, @Status, @IsLocking, @LockEnd);
SELECT SCOPE_IDENTITY();
";

            var id = await GetInvoker().ScalarAsync<int?>(sql, GetInvoker().ToSaveObject(message), cancellationToken)
                .ConfigureAwait(false);
            if (id is null)
                throw new DbUpdateException();

            message.Id = id.ToString();
            return message.Id!;
        }

        public async Task UpdatePublishedAsync(string storageId, string status, int retryCount,
            DateTimeOffset? expireTime,
            CancellationToken cancellationToken = default)
        {
            var sql = $@"
UPDATE {Options.CurrentValue.FullPublishedTableName}
SET status = @status, retryCount = @retryCount, expireTime = @expireTime
WHERE id = @storageId
";

            await GetInvoker()
                .NonQueryAsync(sql, new { status, retryCount, expireTime = expireTime?.GetLongDate() ?? 0, storageId },
                    cancellationToken);
        }

        public async Task UpdateReceivedAsync(string storageId, string status, int retryCount,
            DateTimeOffset? expireTime,
            CancellationToken cancellationToken = default)
        {
            if (storageId == null) throw new ArgumentNullException(nameof(storageId));
            var sql = $@"
UPDATE {Options.CurrentValue.FullReceivedTableName}
SET status = @status, retryCount = @retryCount, expireTime = @expireTime
WHERE id = @storageId
";

            await GetInvoker()
                .NonQueryAsync(sql, new { status, retryCount, expireTime = expireTime?.GetLongDate() ?? 0, storageId },
                    cancellationToken);
        }

        public async Task<bool> TryLockPublishedAsync(string storageId, DateTimeOffset lockEndAt,
            CancellationToken cancellationToken)
        {
            if (lockEndAt <= DateTimeOffset.Now)
                throw new ArgumentOutOfRangeException(nameof(lockEndAt));
            var nowLong = DateTimeOffset.Now.GetLongDate();

            var sql = $@"
UPDATE {Options.CurrentValue.FullPublishedTableName}
SET isLocking = 1, lockEnd = @lockEndAt
WHERE id = @storageId AND (isLocking = 0 OR lockEnd < @nowLong)
";

            var row = await GetInvoker()
                .NonQueryAsync(sql, new { lockEndAt = lockEndAt.GetLongDate(), storageId, nowLong }, cancellationToken)
                .ConfigureAwait(false);
            return row == 1;
        }

        public async Task<bool> TryLockReceivedAsync(string storageId, DateTimeOffset lockEndAt,
            CancellationToken cancellationToken)
        {
            if (lockEndAt <= DateTimeOffset.Now)
                throw new ArgumentOutOfRangeException(nameof(lockEndAt));
            var nowLong = DateTimeOffset.Now.GetLongDate();

            var sql = $@"
UPDATE {Options.CurrentValue.FullReceivedTableName}
SET isLocking = 1, lockEnd = @lockEndAt
WHERE id = @storageId AND (isLocking = 0 OR lockEnd < @nowLong)
";
            var row = await GetInvoker()
                .NonQueryAsync(sql, new { lockEndAt = lockEndAt.GetLongDate(), storageId, nowLong }, cancellationToken)
                .ConfigureAwait(false);
            return row == 1;
        }

        public async Task DeleteExpiresAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.Now.GetLongDate();
            var sql = $@"
DELETE FROM {Options.CurrentValue.FullPublishedTableName} WHERE expireTime > 0 AND expireTime < {now} AND status = '{MessageStatus.Succeeded}';
DELETE FROM {Options.CurrentValue.FullReceivedTableName} WHERE expireTime > 0 AND expireTime < {now} AND status = '{MessageStatus.Succeeded}';
";
            await GetInvoker().NonQueryAsync(sql, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task<List<MessageStorageModel>> GetPublishedMessagesOfNeedRetryAsync(
            int count,
            int delayRetrySecond,
            int maxFailedRetryCount,
            string environment,
            CancellationToken cancellationToken = default)
        {
            var createTimeLimit = DateTimeOffset.Now.AddSeconds(-delayRetrySecond).GetLongDate();
            var now = DateTimeOffset.Now;
            var nowLong = now.GetLongDate();

            var sql = $@"
SELECT  TOP {count} * FROM {Options.CurrentValue.FullPublishedTableName}
WHERE
    environment = '{environment}'
    AND createTime < {createTimeLimit}
    AND retryCount < {maxFailedRetryCount}
    AND (isLocking = 0 OR lockEnd < {nowLong})
    AND (status = '{MessageStatus.Scheduled}' OR status = '{MessageStatus.Failed}')
;
";

            return await GetInvoker().QueryModelAsync(sql, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task<List<MessageStorageModel>> GetReceivedMessagesOfNeedRetryAsync(
            int count,
            int delayRetrySecond,
            int maxFailedRetryCount,
            string environment,
            CancellationToken cancellationToken = default)
        {
            var createTimeLimit = DateTimeOffset.Now.AddSeconds(-delayRetrySecond).GetLongDate();
            var now = DateTimeOffset.Now;
            var nowLong = now.GetLongDate();

            var sql = $@"
SELECT TOP {count} * FROM {Options.CurrentValue.FullReceivedTableName}
WHERE
    environment = '{environment}'
    AND ((isDelay = 0 AND createTime < {createTimeLimit}) OR (isDelay = 1 AND delayAt <= {nowLong} ))
    AND retryCount < {maxFailedRetryCount}
    AND (isLocking = 0 OR lockEnd < {nowLong})
    AND (status = '{MessageStatus.Scheduled}' OR status = '{MessageStatus.Failed}')
;
";

            return await GetInvoker().QueryModelAsync(sql, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Dictionary<string, int>> GetPublishedMessageStatusCountsAsync(
            CancellationToken cancellationToken)
        {
            var sql = $@"
SELECT status, COUNT(1) AS c FROM {Options.CurrentValue.FullPublishedTableName} GROUP BY status;
";

            using var connection = CreateConnection();
            var list = (await connection.QueryAsync(sql).ConfigureAwait(false))?.ToList();
            if (list.IsNullOrEmpty())
                return new Dictionary<string, int>();
            return list!.ToDictionary(r => (string)r.status, r => (int)r.c);
        }

        public async Task<Dictionary<string, int>> GetReceivedMessageStatusCountAsync(
            CancellationToken cancellationToken)
        {
            var sql = $@"
SELECT status, COUNT(1) AS c FROM {Options.CurrentValue.FullReceivedTableName} GROUP BY status;
";
            using var connection = CreateConnection();
            var list = (await connection.QueryAsync(sql).ConfigureAwait(false))?.ToList();
            if (list.IsNullOrEmpty())
                return new Dictionary<string, int>();
            return list!.ToDictionary(r => (string)r.status, r => (int)r.c);
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(ConnectionString.ConnectionString);
        }
    }
}