using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shashlik.EventBus.Utils;

// ReSharper disable MemberCanBePrivate.Global

namespace Shashlik.EventBus.RelationDbStorage;

/// <summary>
/// FreeSql 跨方言的 IMessageStorage 默认实现。直接使用,不再需要 4 个方言包各自继承。
/// </summary>
internal class RelationDbMessageStorage : IMessageStorage
{
    public RelationDbMessageStorage(IFreeSqlFactory freeSqlFactory)
    {
        FreeSql = freeSqlFactory.Instance();
    }

    protected IFreeSql FreeSql { get; }

    public virtual async ValueTask<bool> IsCommittedAsync(string msgId, CancellationToken cancellationToken = default)
    {
        return await FreeSql.Select<RelationDbMessageStoragePublishedModel>()
            .Where(r => r.MsgId == msgId)
            .AnyAsync(cancellationToken);
    }

    public virtual async Task<MessageStorageModel?> FindPublishedByMsgIdAsync(string msgId,
        CancellationToken cancellationToken)
    {
        var entity = await FreeSql.Select<RelationDbMessageStoragePublishedModel>()
            .Where(r => r.MsgId == msgId)
            .FirstAsync(cancellationToken);
        return entity?.ToModel();
    }

    public virtual async Task<MessageStorageModel?> FindPublishedByIdAsync(long storageId,
        CancellationToken cancellationToken)
    {
        var id = storageId.ParseTo<long>();
        var entity = await FreeSql.Select<RelationDbMessageStoragePublishedModel>(id)
            .FirstAsync(cancellationToken);
        return entity?.ToModel();
    }

    public virtual async Task<MessageStorageModel?> FindReceivedByMsgIdAsync(string msgId,
        EventHandlerDescriptor eventHandlerDescriptor,
        CancellationToken cancellationToken = default)
    {
        var entity = await FreeSql.Select<RelationDbMessageStorageReceivedModel>()
            .Where(r => r.MsgId == msgId && r.EventHandlerName == eventHandlerDescriptor.EventHandlerName)
            .FirstAsync(cancellationToken);
        return entity?.ToModel();
    }

    public virtual async Task<MessageStorageModel?> FindReceivedByIdAsync(long storageId,
        CancellationToken cancellationToken)
    {
        var id = storageId.ParseTo<long>();
        var entity = await FreeSql.Select<RelationDbMessageStorageReceivedModel>(id)
            .FirstAsync(cancellationToken);
        return entity?.ToModel();
    }

    public virtual async Task<List<MessageStorageModel>> SearchPublishedAsync(string environment,
        DateTimeOffset beginTime,
        DateTimeOffset endTime, string? eventName, string? status,
        int skip, int take, CancellationToken cancellationToken)
    {
        var begin = beginTime.GetLongDate();
        var end = endTime.GetLongDate();
        var query = FreeSql.Select<RelationDbMessageStoragePublishedModel>()
            .Where(r => r.CreateTimeTicks >= begin && r.CreateTimeTicks <= end && r.Environment == environment)
            .WhereIf(!string.IsNullOrEmpty(eventName), r => r.EventName == eventName)
            .WhereIf(!string.IsNullOrEmpty(status), r => r.Status == status)
            .OrderBy(r => r.CreateTimeTicks)
            .Skip(skip)
            .Take(take);

        var result = await query.ToListAsync(cancellationToken);
        return result.Select(r => r.ToModel()).ToList();
    }

    public virtual async Task<List<MessageStorageModel>> SearchReceivedAsync(string environment,
        DateTimeOffset beginTime,
        DateTimeOffset endTime, string? eventName,
        string? eventHandlerName,
        string? status, int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var begin = beginTime.GetLongDate();
        var end = endTime.GetLongDate();
        var query = FreeSql.Select<RelationDbMessageStorageReceivedModel>()
            .Where(r => r.CreateTimeTicks >= begin && r.CreateTimeTicks <= end && r.Environment == environment)
            .WhereIf(!string.IsNullOrEmpty(eventName), r => r.EventName == eventName)
            .WhereIf(!string.IsNullOrEmpty(eventHandlerName), r => r.EventHandlerName == eventHandlerName)
            .WhereIf(!string.IsNullOrEmpty(status), r => r.Status == status)
            .OrderBy(r => r.CreateTimeTicks)
            .Skip(skip)
            .Take(take);

        var result = await query.ToListAsync(cancellationToken);
        return result.Select(r => r.ToModel()).ToList();
    }

    public virtual async Task<long> SavePublishedAsync(MessageStorageModel message,
        ITransactionContext? transactionContext,
        CancellationToken cancellationToken = default)
    {
        var entity = message.ToPublishedSaveObject();
        var sql = FreeSql.InsertOrUpdate<RelationDbMessageStoragePublishedModel>()
            .SetSource(entity, r => r.MsgId)
            .IfExistsDoNothing()
            .ToSql();

        var dbTransaction = (transactionContext as RelationDbStorageTransactionContext)?.DbTransaction as DbTransaction;
        var dbConnection = dbTransaction?.Connection;
        // 新增连接和事务判断，复用已有数据库连接
        if (dbConnection is not null && dbTransaction is not null)
        {
            await FreeSql.Ado.ExecuteNonQueryAsync(dbConnection, dbTransaction, sql,
                cancellationToken: cancellationToken);
        }
        else
        {
            await FreeSql.Ado.ExecuteNonQueryAsync(sql, cancellationToken: cancellationToken);
        }

        return message.Id;
    }

    public virtual async Task<long> SaveReceivedAsync(MessageStorageModel message,
        CancellationToken cancellationToken = default)
    {
        var entity = message.ToReceivedSaveObject();

        await FreeSql.InsertOrUpdate<RelationDbMessageStorageReceivedModel>()
            .SetSource(entity, r => new { r.MsgId, r.EventHandlerName })
            .IfExistsDoNothing()
            .ExecuteAffrowsAsync(cancellationToken);

        return message.Id;
    }

    public virtual async Task UpdatePublishedAsync(long storageId, string status, int retryCount,
        DateTimeOffset? expireTime, CancellationToken cancellationToken = default)
    {
        var id = storageId.ParseTo<long>();
        await FreeSql.Update<RelationDbMessageStoragePublishedModel>(id)
            .Set(r => r.Status, status)
            .Set(r => r.RetryCount, retryCount)
            .Set(r => r.ExpireTimeTicks, expireTime?.GetLongDate())
            .ExecuteAffrowsAsync(cancellationToken);
    }

    public virtual async Task UpdateReceivedAsync(long storageId, string status, int retryCount,
        DateTimeOffset? expireTime,
        CancellationToken cancellationToken = default)
    {
        var id = storageId.ParseTo<long>();
        await FreeSql.Update<RelationDbMessageStorageReceivedModel>(id)
            .Set(r => r.Status, status)
            .Set(r => r.RetryCount, retryCount)
            .Set(r => r.ExpireTimeTicks, expireTime?.GetLongDate())
            .ExecuteAffrowsAsync(cancellationToken);
    }

    public virtual async Task<bool> TryLockPublishedAsync(long storageId, DateTimeOffset lockEndAt,
        CancellationToken cancellationToken)
    {
        var id = storageId.ParseTo<long>();
        var now = DateTimeOffset.UtcNow.GetLongDate();
        return await FreeSql.Update<RelationDbMessageStoragePublishedModel>(id)
            .Where(r => !r.IsLocking || r.LockEndTicks == null || r.LockEndTicks < now)
            .Set(r => r.IsLocking, true)
            .Set(r => r.LockEndTicks, lockEndAt.GetLongDate())
            .ExecuteAffrowsAsync(cancellationToken) == 1;
    }

    public virtual async Task<bool> TryLockReceivedAsync(long storageId, DateTimeOffset lockEndAt,
        CancellationToken cancellationToken)
    {
        var id = storageId.ParseTo<long>();
        var now = DateTimeOffset.UtcNow.GetLongDate();
        return await FreeSql.Update<RelationDbMessageStorageReceivedModel>(id)
            .Where(r => !r.IsLocking || r.LockEndTicks == null || r.LockEndTicks < now)
            .Set(r => r.IsLocking, true)
            .Set(r => r.LockEndTicks, lockEndAt.GetLongDate())
            .ExecuteAffrowsAsync(cancellationToken) == 1;
    }

    public virtual async Task DeleteExpiresAsync(int retryFailedMax, CancellationToken cancellationToken = default)
    {
        // 分批删除,避免大表上单条 DELETE 锁住表/分区太久。批量大小 1000,可以根据
        // 业务量调整。循环退出条件:某次循环无行被删除(全部过期清理完毕)。
        // FreeSql 的 IDelete<T> 没有稳定的 Limit 支持,所以用"先 select id 列表,再
        // 按 id 删"两步走。
        const int batchSize = 1000;
        var now = DateTimeOffset.UtcNow.GetLongDate();

        while (!cancellationToken.IsCancellationRequested)
        {
            var ids = await FreeSql.Select<RelationDbMessageStoragePublishedModel>()
                .Where(r => r.ExpireTimeTicks != null && r.ExpireTimeTicks < now && r.Status == MessageStatus.Succeeded)
                .Limit(batchSize)
                .ToListAsync(r => r.Id, cancellationToken);

            var idsFailed = await FreeSql.Select<RelationDbMessageStoragePublishedModel>()
                .Where(r => r.ExpireTimeTicks != null && r.ExpireTimeTicks < now && r.Status == MessageStatus.Failed &&
                            r.RetryCount >= retryFailedMax)
                .Limit(batchSize)
                .ToListAsync(r => r.Id, cancellationToken);
            if (idsFailed.Count > 0)
                ids.AddRange(idsFailed);
            if (ids.Count == 0) break;
            await FreeSql.Delete<RelationDbMessageStoragePublishedModel>()
                .Where(r => ids.Contains(r.Id))
                .ExecuteAffrowsAsync(cancellationToken);
            if (ids.Count < batchSize) break;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var ids = await FreeSql.Select<RelationDbMessageStorageReceivedModel>()
                .Where(r => r.ExpireTimeTicks != null && r.ExpireTimeTicks < now && r.Status == MessageStatus.Succeeded)
                .Limit(batchSize)
                .ToListAsync(r => r.Id, cancellationToken);

            var idsFailed = await FreeSql.Select<RelationDbMessageStorageReceivedModel>()
                .Where(r => r.ExpireTimeTicks != null && r.ExpireTimeTicks < now && r.Status == MessageStatus.Failed &&
                            r.RetryCount >= retryFailedMax)
                .Limit(batchSize)
                .ToListAsync(r => r.Id, cancellationToken);
            if (idsFailed.Count > 0)
                ids.AddRange(idsFailed);

            if (ids.Count == 0) break;
            await FreeSql.Delete<RelationDbMessageStorageReceivedModel>()
                .Where(r => ids.Contains(r.Id))
                .ExecuteAffrowsAsync(cancellationToken);
            if (ids.Count < batchSize) break;
        }
    }

    public virtual async Task<List<MessageStorageModel>> GetPublishedMessagesOfNeedRetryAsync(
        int count,
        int delayRetrySecond,
        int maxFailedRetryCount,
        string environment,
        CancellationToken cancellationToken = default)
    {
        var createTimeLimit = DateTimeOffset.UtcNow.AddSeconds(-delayRetrySecond).GetLongDate();
        var now = DateTimeOffset.UtcNow.GetLongDate();
        return await FreeSql.Select<RelationDbMessageStoragePublishedModel>()
            .Where(r => r.Environment == environment &&
                        r.Status.In(MessageStatus.Scheduled, MessageStatus.Failed) &&
                        r.CreateTimeTicks < createTimeLimit &&
                        r.RetryCount < maxFailedRetryCount &&
                        (!r.IsLocking || r.LockEndTicks == null || r.LockEndTicks < now))
            .OrderByDescending(r => r.CreateTimeTicks)
            .Limit(count)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => t.Result.Select(r => r.ToModel()).ToList(), cancellationToken);
        ;
    }

    public virtual async Task<List<MessageStorageModel>> GetReceivedMessagesOfNeedRetryAsync(
        int count,
        int delayRetrySecond,
        int maxFailedRetryCount,
        string environment,
        CancellationToken cancellationToken = default)
    {
        var createTimeLimit = DateTimeOffset.UtcNow.AddSeconds(-delayRetrySecond).GetLongDate();
        var now = DateTimeOffset.UtcNow.GetLongDate();
        // 见 GetPublishedMessagesOfNeedRetryAsync 注释:内层 UNION ALL 子查询必须各自 Limit
        // 普通消息
        var normalQuery = FreeSql.Select<RelationDbMessageStorageReceivedModel>()
            .Where(r => r.Environment == environment &&
                        r.Status.In(MessageStatus.Scheduled, MessageStatus.Failed) &&
                        r.IsDelay == false &&
                        r.CreateTimeTicks < createTimeLimit &&
                        r.RetryCount < maxFailedRetryCount &&
                        (!r.IsLocking || r.LockEndTicks == null || r.LockEndTicks < now))
            .OrderByDescending(r => r.CreateTimeTicks)
            .Limit(count);

        // 延迟消息
        var delayMax = now + delayRetrySecond + (int)(delayRetrySecond * 0.2);
        var delayQuery = FreeSql.Select<RelationDbMessageStorageReceivedModel>()
            .Where(r => r.Environment == environment &&
                        r.Status.In(MessageStatus.Scheduled, MessageStatus.Failed) &&
                        r.IsDelay == true &&
                        r.DelayAtTicks <= delayMax &&
                        r.RetryCount < maxFailedRetryCount &&
                        (!r.IsLocking || r.LockEndTicks == null || r.LockEndTicks < now))
            .OrderByDescending(r => r.CreateTimeTicks)
            .Limit(count);

        // 合并并排序、限制数量
        return await normalQuery
            .UnionAll(delayQuery)
            .OrderByDescending(r => r.CreateTimeTicks)
            .Limit(count)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => t.Result.Select(r => r.ToModel()).ToList(), cancellationToken);
    }

    public virtual async Task<int> CountPublishedAsync(
        string environment, DateTimeOffset beginTime, DateTimeOffset endTime,
        string? eventName, string? status, CancellationToken cancellationToken)
    {
        var begin = beginTime.GetLongDate();
        var end = endTime.GetLongDate();
        return (int)await FreeSql.Select<RelationDbMessageStoragePublishedModel>()
            .Where(r => r.CreateTimeTicks >= begin && r.CreateTimeTicks <= end && r.Environment == environment)
            .WhereIf(!string.IsNullOrEmpty(eventName), r => r.EventName == eventName)
            .WhereIf(!string.IsNullOrEmpty(status), r => r.Status == status)
            .CountAsync(cancellationToken);
    }

    public virtual async Task<int> CountReceivedAsync(
        string environment, DateTimeOffset beginTime, DateTimeOffset endTime,
        string? eventName, string? eventHandlerName, string? status,
        CancellationToken cancellationToken)
    {
        var begin = beginTime.GetLongDate();
        var end = endTime.GetLongDate();
        return (int)await FreeSql.Select<RelationDbMessageStorageReceivedModel>()
            .Where(r => r.CreateTimeTicks >= begin && r.CreateTimeTicks <= end && r.Environment == environment)
            .WhereIf(!string.IsNullOrEmpty(eventName), r => r.EventName == eventName)
            .WhereIf(!string.IsNullOrEmpty(eventHandlerName), r => r.EventHandlerName == eventHandlerName)
            .WhereIf(!string.IsNullOrEmpty(status), r => r.Status == status)
            .CountAsync(cancellationToken);
    }
}