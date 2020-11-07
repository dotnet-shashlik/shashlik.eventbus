using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using Shashlik.Utils.Extensions;

// ReSharper disable ConvertIfStatementToSwitchExpression

// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable RedundantIfElseBlock

namespace Shashlik.EventBus.MySql
{
    public class MySqlMessageStorage : IMessageStorage
    {
        public MySqlMessageStorage(IOptionsMonitor<EventBusMySqlOptions> options, IConnectionString connectionString)
        {
            Options = options;
            ConnectionString = connectionString;
        }

        private IOptionsMonitor<EventBusMySqlOptions> Options { get; }
        private IConnectionString ConnectionString { get; }

        public async ValueTask<bool> ExistsPublishMessage(string msgId, CancellationToken cancellationToken = default)
        {
            var sql = $@"
SELECT COUNT(`msgId`) FROM `{Options.CurrentValue.PublishTableName}` WHERE `msgId`='{msgId}';";

            var count = (await SqlScalar(sql, cancellationToken))?.ParseTo<int>() ?? 0;
            return count > 0;
        }

        public async ValueTask<bool> ExistsReceiveMessage(string msgId, CancellationToken cancellationToken = default)
        {
            var sql = $@"
SELECT COUNT(`msgId`) FROM `{Options.CurrentValue.ReceiveTableName}` WHERE `msgId`='{msgId}';";

            var count = (await SqlScalar(sql, cancellationToken))?.ParseTo<int>() ?? 0;
            return count > 0;
        }

        public async Task<MessageStorageModel> FindPublishedById(string id,
            CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT * FROM `{Options.CurrentValue.PublishTableName}` WHERE `msgId`='{id}';";

            var table = await SqlQuery(sql, cancellationToken);
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
            var sql = $"SELECT * FROM `{Options.CurrentValue.PublishTableName}` WHERE `msgId`='{id}';";

            var table = await SqlQuery(sql, cancellationToken);
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
INSERT INTO `{Options.CurrentValue.PublishTableName}`
(`msgId`, `environment`, `createTime`, `delayAt`, `expireTime`, `eventName`, `eventBody`, `eventItems`, `retryCount`, `status`, `isLocking`, `lockEnd`)
VALUES(@msgId, @environment, @createTime, @delayAt, @expireTime, @eventName, @eventBody, @eventItems, @retryCount, @status, @isLocking, @lockEnd);
";

            var parameters = new[]
            {
                new MySqlParameter("@msgId", MySqlDbType.VarChar) {Value = message.MsgId},
                new MySqlParameter("@environment", MySqlDbType.VarChar) {Value = message.Environment},
                new MySqlParameter("@createTime", MySqlDbType.Int64) {Value = message.CreateTime.GetLongDate()},
                new MySqlParameter("@delayAt", MySqlDbType.Int64) {Value = message.DelayAt?.GetLongDate() ?? 0},
                new MySqlParameter("@expireTime", MySqlDbType.Int64) {Value = message.ExpireTime?.GetLongDate() ?? 0},
                new MySqlParameter("@eventName", MySqlDbType.VarChar) {Value = message.EventName},
                new MySqlParameter("@eventBody", MySqlDbType.LongText) {Value = message.EventBody},
                new MySqlParameter("@eventItems", MySqlDbType.LongText) {Value = message.EventItems},
                new MySqlParameter("@retryCount", MySqlDbType.Int32) {Value = message.RetryCount},
                new MySqlParameter("@status", MySqlDbType.VarChar) {Value = message.Status},
                new MySqlParameter("@isLocking", MySqlDbType.Byte) {Value = message.IsLocking ? 1 : 0},
                new MySqlParameter("@lockEnd", MySqlDbType.Int64) {Value = message.LockEnd?.GetLongDate() ?? 0},
            };

            await NonQuery(transactionContext, sql, parameters, cancellationToken);
        }

        public async Task SaveReceived(MessageStorageModel message, CancellationToken cancellationToken = default)
        {
            var sql = $@"
INSERT INTO `{Options.CurrentValue.ReceiveTableName}`
(`msgId`, `environment`, `createTime`, `delayAt`, `expireTime`, `eventName`, `eventHandlerName`, `eventBody`, `eventItems`, `retryCount`, `status`, `isLocking`, `lockEnd`)
VALUES(@msgId, @environment, @createTime, @delayAt, @expireTime, @eventName, @eventHandlerName, @eventBody, @eventItems, @retryCount, @status, @isLocking, @lockEnd);
";

            var parameters = new[]
            {
                new MySqlParameter("@msgId", MySqlDbType.VarChar) {Value = message.MsgId},
                new MySqlParameter("@environment", MySqlDbType.VarChar) {Value = message.Environment},
                new MySqlParameter("@createTime", MySqlDbType.Int64) {Value = message.CreateTime.GetLongDate()},
                new MySqlParameter("@delayAt", MySqlDbType.Int64) {Value = message.DelayAt?.GetLongDate() ?? 0},
                new MySqlParameter("@expireTime", MySqlDbType.Int64) {Value = message.ExpireTime?.GetLongDate() ?? 0},
                new MySqlParameter("@eventName", MySqlDbType.VarChar) {Value = message.EventName},
                new MySqlParameter("@eventHandlerName", MySqlDbType.VarChar) {Value = message.EventHandlerName},
                new MySqlParameter("@eventBody", MySqlDbType.LongText) {Value = message.EventBody},
                new MySqlParameter("@eventItems", MySqlDbType.LongText) {Value = message.EventItems},
                new MySqlParameter("@retryCount", MySqlDbType.Int32) {Value = message.RetryCount},
                new MySqlParameter("@status", MySqlDbType.VarChar) {Value = message.Status},
                new MySqlParameter("@isLocking", MySqlDbType.Byte) {Value = message.IsLocking ? 1 : 0},
                new MySqlParameter("@lockEnd", MySqlDbType.Int64) {Value = message.LockEnd?.GetLongDate() ?? 0},
            };

            await NonQuery(sql, parameters, cancellationToken);
        }

        public async Task UpdatePublished(string msgId, string status, int retryCount, DateTimeOffset? expireTime,
            CancellationToken cancellationToken = default)
        {
            var sql = $@"
UPDATE `{Options.CurrentValue.PublishTableName}`
SET `status` = '{status}', `retryCount` = {retryCount}, `expireTime` = {expireTime?.GetLongDate() ?? 0}
WHERE `msgId` = '{msgId}'
";

            await NonQuery(sql, null, cancellationToken);
        }

        public async Task UpdateReceived(string msgId, string status, int retryCount, DateTimeOffset? expireTime,
            CancellationToken cancellationToken = default)
        {
            var sql = $@"
UPDATE `{Options.CurrentValue.ReceiveTableName}`
SET `status` = '{status}', `retryCount` = {retryCount}, `expireTime` = {expireTime?.GetLongDate() ?? 0}
WHERE `msgId` = '{msgId}'
";
            await NonQuery(sql, null, cancellationToken);
        }

        public async Task DeleteExpires(CancellationToken cancellationToken = default)
        {
            var now = DateTime.Now.GetLongDate();
            var sql = $@"
DELETE FROM `{Options.CurrentValue.PublishTableName}` WHERE `expireTime` != 0 AND `expireTime` < {now} AND `status` != '{MessageStatus.Scheduled}';
DELETE FROM `{Options.CurrentValue.ReceiveTableName}` WHERE `expireTime` != 0 AND `expireTime` < {now} AND `status` != '{MessageStatus.Scheduled}';
";
            await NonQuery(sql, null, cancellationToken);
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
SELECT * FROM `{Options.CurrentValue.PublishTableName}`
WHERE
    `environment` = '{environment}'
    AND `createTime` < {createTimeLimit}
    AND `retryCount` < {maxFailedRetryCount}
    AND (`isLocking` = 0 OR `lockEnd` < {nowLong})
    AND (`status` = '{MessageStatus.Scheduled}' OR `status` = '{MessageStatus.Failed}')
LIMIT {count};
";

            var table = await SqlQuery(sql, cancellationToken);
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
UPDATE `{Options.CurrentValue.PublishTableName}`
SET `isLocking` = 1, `lockEnd` = {lockEnd}
WHERE `msgId` IN ({ids}) AND (`isLocking` = 0 OR `lockEnd` < {nowLong});
";
            var rows = await NonQuery(updateSql, null, cancellationToken);
            return rows != list.Count ? new List<MessageStorageModel>() : list;
        }

        public async Task<List<MessageStorageModel>> GetReceivedMessagesOfNeedRetryAndLock(int count,
            int delayRetrySecond,
            int maxFailedRetryCount, string environment,
            int lockSecond, CancellationToken cancellationToken = default)
        {
            var createTimeLimit = DateTime.Now.AddSeconds(-delayRetrySecond).GetLongDate();
            var now = DateTime.Now;
            var nowLong = now.GetLongDate();

            var sql = $@"
SELECT * FROM `{Options.CurrentValue.ReceiveTableName}`
WHERE
    `environment` = '{environment}'
    AND `createTime` < {createTimeLimit}
    AND `retryCount` < {maxFailedRetryCount}
    AND (`isLocking` = 0 OR `lockEnd` < {nowLong})
    AND (`status` = '{MessageStatus.Scheduled}' OR `status` = '{MessageStatus.Failed}')
LIMIT {count};
";

            var table = await SqlQuery(sql, cancellationToken);
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
UPDATE `{Options.CurrentValue.ReceiveTableName}`
SET `isLocking` = 1, `lockEnd` = {lockEnd}
WHERE `msgId` IN ({ids}) AND (`isLocking` = 0 OR `lockEnd` < {nowLong});
";
            var rows = await NonQuery(updateSql, null, cancellationToken);
            return rows != list.Count ? new List<MessageStorageModel>() : list;
        }

        private async Task<DataTable> SqlQuery(string sql, CancellationToken cancellationToken = default)
        {
            await using var connection = new MySqlConnection(ConnectionString.ConnectionString);
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
            await using var connection = new MySqlConnection(ConnectionString.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            return await cmd.ExecuteScalarAsync(cancellationToken);
        }

        private async Task<int> NonQuery(string sql, MySqlParameter[] parameter,
            CancellationToken cancellationToken = default)
        {
            await using var connection = new MySqlConnection(ConnectionString.ConnectionString);
            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync(cancellationToken);
            await using var cmd = connection.CreateCommand();
            if (!parameter.IsNullOrEmpty())
                foreach (var mySqlParameter in parameter)
                    cmd.Parameters.Add(mySqlParameter);
            cmd.CommandText = sql;
            return await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task<int> NonQuery(TransactionContext transactionContext, string sql, MySqlParameter[] parameter,
            CancellationToken cancellationToken = default)
        {
            if (transactionContext == null)
                return await NonQuery(sql, parameter, cancellationToken);
            else if (transactionContext.ConnectionInstance is DbContext dbContext)
            {
                var connection = dbContext.Database.GetDbConnection();
                if (connection.State == ConnectionState.Closed)
                    await connection.OpenAsync(cancellationToken);
                await using var cmd = connection.CreateCommand();
                if (!parameter.IsNullOrEmpty())
                    foreach (var mySqlParameter in parameter)
                        cmd.Parameters.Add(mySqlParameter);

                cmd.CommandText = sql;

                if (transactionContext.TransactionInstance == null && dbContext.Database.CurrentTransaction != null)
                    cmd.Transaction = dbContext.Database.CurrentTransaction.GetDbTransaction();
                else if (transactionContext.TransactionInstance is IDbContextTransaction dbContextTransaction)
                    cmd.Transaction = dbContextTransaction.GetDbTransaction();
                else if (transactionContext.TransactionInstance is IDbTransaction dbTransaction)
                    cmd.Transaction = dbTransaction as DbTransaction;
                else
                    throw new InvalidCastException(
                        $"[EventBus] invalid transaction context data. you can use DbContext or DbConnection for ConnectionInstance, and use IDbContextTransaction or IDbTransaction for TransactionInstance.");

                return await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            else if (transactionContext.ConnectionInstance is DbConnection connection)
            {
                if (connection.State == ConnectionState.Closed)
                    await connection.OpenAsync(cancellationToken);
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

                return await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            throw new InvalidCastException(
                $"[EventBus] invalid transaction context data. you can use DbContext or DbConnection for ConnectionInstance, and use IDbContextTransaction or IDbTransaction for TransactionInstance.");
        }
    }
}