using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.RelationDbStorage;

/// <summary>
/// 提供关系型数据库的一些默认方法
/// </summary>
public abstract class RelationDbMessageStorageBase : IMessageStorage
{
    protected virtual MessageStorageModel ToModel(dynamic model)
    {
        return new MessageStorageModel
        {
            Id = InnerExtensions.ParseTo<string>(model.id),
            MsgId = InnerExtensions.ParseTo<string>(model.msgId),
            Environment = InnerExtensions.ParseTo<string>(model.environment),
            CreateTime = ((long)InnerExtensions.ParseTo<long>(model.createTime)).LongToDateTimeOffset()!.Value,
            DelayAt = ((long?)InnerExtensions.ParseTo<long>(model.delayAt))?.LongToDateTimeOffset(),
            ExpireTime = ((long?)InnerExtensions.ParseTo<long>(model.expireTime))?.LongToDateTimeOffset(),
            EventHandlerName = InnerExtensions.ParseTo<string>(model.eventHandlerName),
            EventName = InnerExtensions.ParseTo<string>(model.eventName),
            EventBody = InnerExtensions.ParseTo<string>(model.eventBody),
            EventItems = InnerExtensions.ParseTo<string>(model.eventItems),
            RetryCount = InnerExtensions.ParseTo<int>(model.retryCount),
            Status = InnerExtensions.ParseTo<string>(model.status),
            IsLocking = InnerExtensions.ParseTo<bool>(model.isLocking),
            LockEnd = ((long?)InnerExtensions.ParseTo<long>(model.lockEnd))?.LongToDateTimeOffset(),
        };
    }

    /// <summary>
    /// 创建新的链接
    /// </summary>
    /// <returns></returns>
    protected abstract IDbConnection CreateConnection();

    /// <summary>
    /// 发布消息表名
    /// </summary>
    protected abstract string PublishedTableName { get; }

    /// <summary>
    /// 接收消息表名
    /// </summary>
    protected abstract string ReceivedTableName { get; }

    /// <summary>
    /// 返回自增id的sql语句
    /// </summary>
    protected abstract string ReturnInsertIdSql { get; }

    /// <summary>
    /// sql 标识符 结束字符
    /// </summary>
    protected virtual string SqlTagCharPrefix => "";

    /// <summary>
    /// sql 标识符 开始字符
    /// </summary>
    protected virtual string SqlTagCharSuffix => "";

    /// <summary>
    /// true值
    /// </summary>
    protected virtual string BoolTrueValue => "1";

    /// <summary>
    /// false值
    /// </summary>
    protected virtual string BoolFalseValue => "0";

    /// <summary>
    /// 转换为存储用的模型对象
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    protected virtual object ToSaveObject(MessageStorageModel model)
    {
        return new
        {
            Id = model.Id,
            MsgId = model.MsgId,
            Environment = model.Environment,
            CreateTime = model.CreateTime.GetLongDate(),
            DelayAt = model.DelayAt?.GetLongDate() ?? 0,
            ExpireTime = model.ExpireTime?.GetLongDate() ?? 0,
            EventHandlerName = model.EventHandlerName,
            EventName = model.EventName,
            EventBody = model.EventBody,
            EventItems = model.EventItems,
            RetryCount = model.RetryCount,
            Status = model.Status,
            IsLocking = model.IsLocking,
            LockEnd = model.LockEnd?.GetLongDate() ?? 0,
            IsDelay = model.DelayAt.HasValue
        };
    }

    protected virtual async Task<MessageStorageModel?> QueryOneModelAsync(string sql, object? ps,
        CancellationToken cancellationToken = default)
    {
        using var dbConnection = CreateConnection();
        dbConnection.Open();
        var model = await dbConnection.QueryFirstOrDefaultAsync(sql, ps).ConfigureAwait(false);
        return model is null ? null : (MessageStorageModel?)ToModel(model);
    }

    protected virtual async Task<List<MessageStorageModel>> QueryModelAsync(string sql, object? ps,
        CancellationToken cancellationToken = default)
    {
        using var dbConnection = CreateConnection();
        dbConnection.Open();
        var list = await dbConnection.QueryAsync(sql, ps).ConfigureAwait(false);
        return list.Select(ToModel).ToList();
    }

    protected virtual async Task<TRes> ScalarAsync<TRes>(string sql, object? ps,
        CancellationToken cancellationToken = default)
    {
        using var dbConnection = CreateConnection();
        dbConnection.Open();
        return await dbConnection.ExecuteScalarAsync<TRes>(sql, ps).ConfigureAwait(false);
    }

    protected virtual async Task<int> NonQueryAsync(string sql, object? ps,
        CancellationToken cancellationToken = default)
    {
        using var dbConnection = CreateConnection();
        dbConnection.Open();
        return await dbConnection.ExecuteAsync(sql, ps).ConfigureAwait(false);
    }

    protected virtual async Task<TRes> ScalarAsync<TRes>(ITransactionContext? transactionContext, string sql,
        object? ps, CancellationToken cancellationToken = default)
    {
        if (transactionContext is null or XaTransactionContext)
            return await ScalarAsync<TRes>(sql, ps, cancellationToken).ConfigureAwait(false);

        if (transactionContext is not RelationDbStorageTransactionContext relationDbStorageTransactionContext)
            throw new InvalidCastException(
                $"[EventBus]Storage only support transaction context of {typeof(RelationDbStorageTransactionContext)}");

        var connection = relationDbStorageTransactionContext.DbTransaction.Connection;
        if (connection!.State != ConnectionState.Open)
            connection.Open();

        return await connection
            .ExecuteScalarAsync<TRes>(sql, ps, transaction: relationDbStorageTransactionContext.DbTransaction)
            .ConfigureAwait(false);
    }

    public virtual async ValueTask<bool> IsCommittedAsync(string msgId, CancellationToken cancellationToken = default)
    {
        var sql =
            $@"SELECT 1 FROM {PublishedTableName} WHERE {SqlTagCharPrefix}msgId{SqlTagCharSuffix} = @msgId LIMIT 1;";
        var count = await ScalarAsync<int>(sql, new { msgId }, cancellationToken);
        return count > 0;
    }

    public virtual async Task<MessageStorageModel?> FindPublishedByMsgIdAsync(string msgId,
        CancellationToken cancellationToken)
    {
        var sql = $"SELECT * FROM {PublishedTableName} WHERE {SqlTagCharPrefix}msgId{SqlTagCharSuffix} = @msgId;";
        return await QueryOneModelAsync(sql, new { msgId }, cancellationToken)
            .ConfigureAwait(false);
    }

    public virtual async Task<MessageStorageModel?> FindPublishedByIdAsync(string storageId,
        CancellationToken cancellationToken)
    {
        var sql = $"SELECT * FROM {PublishedTableName} WHERE {SqlTagCharPrefix}id{SqlTagCharSuffix} = @storageId;";
        return await QueryOneModelAsync(sql, new { storageId = storageId.ParseTo<int>() }, cancellationToken);
    }

    public virtual async Task<MessageStorageModel?> FindReceivedByMsgIdAsync(string msgId,
        EventHandlerDescriptor eventHandlerDescriptor,
        CancellationToken cancellationToken = default)
    {
        var sql =
            $"SELECT * FROM {ReceivedTableName} WHERE {SqlTagCharPrefix}msgId{SqlTagCharSuffix} = @msgId AND {SqlTagCharPrefix}eventHandlerName{SqlTagCharSuffix} = @eventHandlerName;";

        return await QueryOneModelAsync(sql,
                new { msgId, eventHandlerName = eventHandlerDescriptor.EventHandlerName }, cancellationToken)
            .ConfigureAwait(false);
    }

    public virtual async Task<MessageStorageModel?> FindReceivedByIdAsync(string storageId,
        CancellationToken cancellationToken)
    {
        var sql = $"SELECT * FROM {ReceivedTableName} WHERE {SqlTagCharPrefix}id{SqlTagCharSuffix} = @storageId;";
        return await QueryOneModelAsync(sql, new { storageId = storageId.ParseTo<int>() }, cancellationToken)
            .ConfigureAwait(false);
    }

    public virtual async Task<List<MessageStorageModel>> SearchPublishedAsync(string? eventName, string? status,
        int skip, int take, CancellationToken cancellationToken)
    {
        var where = new StringBuilder();
        if (!eventName.IsNullOrWhiteSpace())
        {
            where.Append($" AND {SqlTagCharPrefix}eventName{SqlTagCharSuffix} = @eventName");
        }

        if (!status.IsNullOrWhiteSpace())
        {
            where.Append($" AND {SqlTagCharPrefix}status{SqlTagCharSuffix} = @status");
        }

        var sql = $@"
SELECT * FROM {PublishedTableName}
WHERE 
    1 = 1{where}
ORDER BY {SqlTagCharPrefix}createTime{SqlTagCharSuffix} DESC
LIMIT {take} OFFSET {skip};
";

        return await QueryModelAsync(sql, new { eventName, status }, cancellationToken)
            .ConfigureAwait(false);
    }

    public virtual async Task<List<MessageStorageModel>> SearchReceivedAsync(string? eventName,
        string? eventHandlerName,
        string? status, int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var where = new StringBuilder();
        if (!eventName.IsNullOrWhiteSpace())
        {
            where.Append($" AND {SqlTagCharPrefix}eventName{SqlTagCharSuffix} = @eventName");
        }

        if (!eventHandlerName.IsNullOrWhiteSpace())
        {
            where.Append($" AND {SqlTagCharPrefix}eventHandlerName{SqlTagCharSuffix} = @eventHandlerName");
        }

        if (!status.IsNullOrWhiteSpace())
        {
            where.Append($" AND {SqlTagCharPrefix}status{SqlTagCharSuffix} = @status");
        }

        var sql = $@"
SELECT * FROM {ReceivedTableName}
WHERE 
    1 = 1{where}
ORDER BY {SqlTagCharPrefix}createTime{SqlTagCharSuffix} DESC
LIMIT {take} OFFSET {skip};
";

        return await QueryModelAsync(sql, new { eventName, eventHandlerName, status }, cancellationToken)
            .ConfigureAwait(false);
    }

    public virtual async Task<string> SavePublishedAsync(MessageStorageModel message,
        ITransactionContext? transactionContext,
        CancellationToken cancellationToken = default)
    {
        var sql = $@"
INSERT INTO {PublishedTableName}
({SqlTagCharPrefix}msgId{SqlTagCharSuffix}, {SqlTagCharPrefix}environment{SqlTagCharSuffix}, {SqlTagCharPrefix}createTime{SqlTagCharSuffix}, {SqlTagCharPrefix}delayAt{SqlTagCharSuffix}, {SqlTagCharPrefix}expireTime{SqlTagCharSuffix}, {SqlTagCharPrefix}eventName{SqlTagCharSuffix}, {SqlTagCharPrefix}eventBody{SqlTagCharSuffix}, {SqlTagCharPrefix}eventItems{SqlTagCharSuffix}, {SqlTagCharPrefix}retryCount{SqlTagCharSuffix}, {SqlTagCharPrefix}status{SqlTagCharSuffix}, {SqlTagCharPrefix}isLocking{SqlTagCharSuffix}, {SqlTagCharPrefix}lockEnd{SqlTagCharSuffix})
VALUES(@MsgId, @Environment, @CreateTime, @DelayAt, @ExpireTime, @EventName, @EventBody, @EventItems, @RetryCount, @Status, @IsLocking, @LockEnd){ReturnInsertIdSql}
;";
        var id = await ScalarAsync<int?>(transactionContext, sql, ToSaveObject(message),
                cancellationToken)
            .ConfigureAwait(false);
        if (id is null)
            throw new EventBusException("[EventBus]Save published message data return null id.");

        message.Id = id.ToString();
        return message.Id!;
    }

    public virtual async Task<string> SaveReceivedAsync(MessageStorageModel message,
        CancellationToken cancellationToken = default)
    {
        var sql = $@"
INSERT INTO {ReceivedTableName}
({SqlTagCharPrefix}msgId{SqlTagCharSuffix}, {SqlTagCharPrefix}environment{SqlTagCharSuffix}, {SqlTagCharPrefix}createTime{SqlTagCharSuffix}, {SqlTagCharPrefix}isDelay{SqlTagCharSuffix}, {SqlTagCharPrefix}delayAt{SqlTagCharSuffix}, {SqlTagCharPrefix}expireTime{SqlTagCharSuffix}, {SqlTagCharPrefix}eventName{SqlTagCharSuffix}, {SqlTagCharPrefix}eventHandlerName{SqlTagCharSuffix}, {SqlTagCharPrefix}eventBody{SqlTagCharSuffix}, {SqlTagCharPrefix}eventItems{SqlTagCharSuffix}, {SqlTagCharPrefix}retryCount{SqlTagCharSuffix}, {SqlTagCharPrefix}status{SqlTagCharSuffix}, {SqlTagCharPrefix}isLocking{SqlTagCharSuffix}, {SqlTagCharPrefix}lockEnd{SqlTagCharSuffix})
VALUES(@MsgId, @Environment, @CreateTime, @IsDelay, @DelayAt, @ExpireTime, @EventName, @EventHandlerName, @EventBody, @EventItems, @RetryCount, @Status, @IsLocking, @LockEnd){ReturnInsertIdSql}
;";

        var id = await ScalarAsync<int?>(sql, ToSaveObject(message), cancellationToken)
            .ConfigureAwait(false);
        if (id is null)
            throw new EventBusException("[EventBus]Save published message data return null id.");

        message.Id = id.ToString();
        return message.Id!;
    }

    public virtual async Task UpdatePublishedAsync(string storageId, string status, int retryCount,
        DateTimeOffset? expireTime,
        CancellationToken cancellationToken = default)
    {
        var sql = $@"
UPDATE {PublishedTableName}
SET {SqlTagCharPrefix}status{SqlTagCharSuffix} = @status, {SqlTagCharPrefix}retryCount{SqlTagCharSuffix} = @retryCount, {SqlTagCharPrefix}expireTime{SqlTagCharSuffix} = @expireTime
WHERE {SqlTagCharPrefix}id{SqlTagCharSuffix} = @storageId
;";

        await NonQueryAsync(sql,
            new
            {
                status, retryCount, expireTime = expireTime?.GetLongDate() ?? 0, storageId = storageId.ParseTo<int>()
            },
            cancellationToken);
    }

    public virtual async Task UpdateReceivedAsync(string storageId, string status, int retryCount,
        DateTimeOffset? expireTime,
        CancellationToken cancellationToken = default)
    {
        if (storageId == null) throw new ArgumentNullException(nameof(storageId));
        var sql = $@"
UPDATE {ReceivedTableName}
SET {SqlTagCharPrefix}status{SqlTagCharSuffix} = @status, {SqlTagCharPrefix}retryCount{SqlTagCharSuffix} = @retryCount, {SqlTagCharPrefix}expireTime{SqlTagCharSuffix} = @expireTime
WHERE {SqlTagCharPrefix}id{SqlTagCharSuffix} = @storageId
;";

        await NonQueryAsync(sql,
            new
            {
                status, retryCount, expireTime = expireTime?.GetLongDate() ?? 0, storageId = storageId.ParseTo<int>()
            },
            cancellationToken);
    }

    public virtual async Task<bool> TryLockPublishedAsync(string storageId, DateTimeOffset lockEndAt,
        CancellationToken cancellationToken)
    {
        if (lockEndAt <= DateTimeOffset.Now)
            throw new ArgumentOutOfRangeException(nameof(lockEndAt));
        var nowLong = DateTimeOffset.Now.GetLongDate();

        var sql = $@"
UPDATE {PublishedTableName}
SET {SqlTagCharPrefix}isLocking{SqlTagCharSuffix} = {BoolTrueValue}, {SqlTagCharPrefix}lockEnd{SqlTagCharSuffix} = @lockEndAt
WHERE {SqlTagCharPrefix}id{SqlTagCharSuffix} = @storageId AND ({SqlTagCharPrefix}isLocking{SqlTagCharSuffix} = {BoolFalseValue} OR {SqlTagCharPrefix}lockEnd{SqlTagCharSuffix} < @nowLong)
;
";

        var row = await NonQueryAsync(sql,
                new { lockEndAt = lockEndAt.GetLongDate(), storageId = storageId.ParseTo<int>(), nowLong },
                cancellationToken)
            .ConfigureAwait(false);
        return row == 1;
    }

    public virtual async Task<bool> TryLockReceivedAsync(string storageId, DateTimeOffset lockEndAt,
        CancellationToken cancellationToken)
    {
        if (lockEndAt <= DateTimeOffset.Now)
            throw new ArgumentOutOfRangeException(nameof(lockEndAt));
        var nowLong = DateTimeOffset.Now.GetLongDate();

        var sql = $@"
UPDATE {ReceivedTableName}
SET {SqlTagCharPrefix}isLocking{SqlTagCharSuffix} = {BoolTrueValue}, {SqlTagCharPrefix}lockEnd{SqlTagCharSuffix} = @lockEndAt
WHERE {SqlTagCharPrefix}id{SqlTagCharSuffix} = @storageId AND ({SqlTagCharPrefix}isLocking{SqlTagCharSuffix} = {BoolFalseValue} OR {SqlTagCharPrefix}lockEnd{SqlTagCharSuffix} < @nowLong)
;
";
        var row = await NonQueryAsync(sql,
                new { lockEndAt = lockEndAt.GetLongDate(), storageId = storageId.ParseTo<int>(), nowLong },
                cancellationToken)
            .ConfigureAwait(false);
        return row == 1;
    }

    public virtual async Task DeleteExpiresAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.Now.GetLongDate();
        var sql = $@"
DELETE FROM {PublishedTableName} WHERE {SqlTagCharPrefix}expireTime{SqlTagCharSuffix} > 0 AND {SqlTagCharPrefix}expireTime{SqlTagCharSuffix} < {now} AND {SqlTagCharPrefix}status{SqlTagCharSuffix} = '{MessageStatus.Succeeded}';
DELETE FROM {ReceivedTableName} WHERE {SqlTagCharPrefix}expireTime{SqlTagCharSuffix} > 0 AND {SqlTagCharPrefix}expireTime{SqlTagCharSuffix} < {now} AND {SqlTagCharPrefix}status{SqlTagCharSuffix} = '{MessageStatus.Succeeded}';";
        await NonQueryAsync(sql, null, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<List<MessageStorageModel>> GetPublishedMessagesOfNeedRetryAsync(
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
SELECT * FROM {PublishedTableName}
WHERE
    {SqlTagCharPrefix}environment{SqlTagCharSuffix} = '{environment}'
    AND {SqlTagCharPrefix}createTime{SqlTagCharSuffix} < {createTimeLimit}
    AND {SqlTagCharPrefix}retryCount{SqlTagCharSuffix} < {maxFailedRetryCount}
    AND ({SqlTagCharPrefix}isLocking{SqlTagCharSuffix} = {BoolFalseValue} OR {SqlTagCharPrefix}lockEnd{SqlTagCharSuffix} < {nowLong})
    AND ({SqlTagCharPrefix}status{SqlTagCharSuffix} = '{MessageStatus.Scheduled}' OR {SqlTagCharPrefix}status{SqlTagCharSuffix} = '{MessageStatus.Failed}')
LIMIT {count}
;";

        return await QueryModelAsync(sql, null, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<List<MessageStorageModel>> GetReceivedMessagesOfNeedRetryAsync(
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
SELECT * FROM {ReceivedTableName}
WHERE
    {SqlTagCharPrefix}environment{SqlTagCharSuffix} = '{environment}'
    AND (({SqlTagCharPrefix}isDelay{SqlTagCharSuffix} = {BoolFalseValue} AND {SqlTagCharPrefix}createTime{SqlTagCharSuffix} < {createTimeLimit}) OR ({SqlTagCharPrefix}isDelay{SqlTagCharSuffix} = {BoolTrueValue} AND {SqlTagCharPrefix}delayAt{SqlTagCharSuffix} <= {nowLong} ))
    AND {SqlTagCharPrefix}retryCount{SqlTagCharSuffix} < {maxFailedRetryCount}
    AND ({SqlTagCharPrefix}isLocking{SqlTagCharSuffix} = {BoolFalseValue} OR {SqlTagCharPrefix}lockEnd{SqlTagCharSuffix} < {nowLong})
    AND ({SqlTagCharPrefix}status{SqlTagCharSuffix} = '{MessageStatus.Scheduled}' OR {SqlTagCharPrefix}status{SqlTagCharSuffix} = '{MessageStatus.Failed}')
LIMIT {count}
;";

        return await QueryModelAsync(sql, null, cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task<Dictionary<string, int>> GetPublishedMessageStatusCountsAsync(
        CancellationToken cancellationToken)
    {
        var sql =
            $@"SELECT {SqlTagCharPrefix}status{SqlTagCharSuffix}, COUNT(1) AS c FROM {PublishedTableName} GROUP BY {SqlTagCharPrefix}status{SqlTagCharSuffix};";

        using var connection = CreateConnection();
        var list = (await connection.QueryAsync(sql).ConfigureAwait(false))?.ToList();
        if (list.IsNullOrEmpty())
            return new Dictionary<string, int>();
        return list!.ToDictionary(r => (string)r.status, r => (int)r.c);
    }

    public virtual async Task<Dictionary<string, int>> GetReceivedMessageStatusCountAsync(
        CancellationToken cancellationToken)
    {
        var sql =
            $@"SELECT {SqlTagCharPrefix}status{SqlTagCharSuffix}, COUNT(1) AS c FROM {ReceivedTableName} GROUP BY {SqlTagCharPrefix}status{SqlTagCharSuffix};";
        using var connection = CreateConnection();
        var list = (await connection.QueryAsync(sql).ConfigureAwait(false))?.ToList();
        if (list.IsNullOrEmpty())
            return new Dictionary<string, int>();
        return list!.ToDictionary(r => (string)r.status, r => (int)r.c);
    }
}