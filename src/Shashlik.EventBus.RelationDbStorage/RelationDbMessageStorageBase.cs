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
public class RelationDbMessageStorage : IMessageStorage
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

    public virtual async Task<MessageStorageModel?> FindPublishedByIdAsync(string storageId,
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

    public virtual async Task<MessageStorageModel?> FindReceivedByIdAsync(string storageId,
        CancellationToken cancellationToken)
    {
        var id = storageId.ParseTo<long>();
        var entity = await FreeSql.Select<RelationDbMessageStorageReceivedModel>(id)
            .FirstAsync(cancellationToken);
        return entity?.ToModel();
    }

    public virtual async Task<List<MessageStorageModel>> SearchPublishedAsync(string? eventName, string? status,
        int skip, int take, CancellationToken cancellationToken)
    {
        var query = FreeSql.Select<RelationDbMessageStoragePublishedModel>()
            .WhereIf(!string.IsNullOrEmpty(eventName), r => r.EventName == eventName)
            .WhereIf(!string.IsNullOrEmpty(status), r => r.Status == status)
            .OrderBy(r => r.CreateTime)
            .Skip(skip)
            .Take(take);

        var result = await query.ToListAsync(cancellationToken);
        return result.Select(r => r.ToModel()).ToList();
    }

    public virtual async Task<List<MessageStorageModel>> SearchReceivedAsync(string? eventName,
        string? eventHandlerName,
        string? status, int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = FreeSql.Select<RelationDbMessageStorageReceivedModel>()
            .WhereIf(!string.IsNullOrEmpty(eventName), r => r.EventName == eventName)
            .WhereIf(!string.IsNullOrEmpty(eventHandlerName), r => r.EventHandlerName == eventHandlerName)
            .WhereIf(!string.IsNullOrEmpty(status), r => r.Status == status)
            .OrderBy(r => r.CreateTime)
            .Skip(skip)
            .Take(take);

        var result = await query.ToListAsync(cancellationToken);
        return result.Select(r => r.ToModel()).ToList();
    }

    public virtual async Task<string> SavePublishedAsync(MessageStorageModel message,
        ITransactionContext? transactionContext,
        CancellationToken cancellationToken = default)
    {
        var entity = message.ToPublishedSaveObject();
        var insert = FreeSql.Insert<RelationDbMessageStoragePublishedModel>()
            .AppendData(entity);

        var dbTransaction = (transactionContext as RelationDbStorageTransactionContext)?.DbTransaction;
        var dbConnection = dbTransaction?.Connection;

        // 新增连接和事务判断，复用已有数据库连接
        if (dbConnection != null)
        {
            insert = insert.WithConnection(dbConnection as DbConnection);
            if (dbTransaction != null)
                insert = insert.WithTransaction(dbTransaction as DbTransaction);
        }

        var id = await insert.ExecuteIdentityAsync(cancellationToken);
        message.Id = id.ToString();
        return message.Id;
    }

    public virtual async Task<string> SaveReceivedAsync(MessageStorageModel message,
        CancellationToken cancellationToken = default)
    {
        var entity = message.ToReceivedSaveObject();

        // InsertOrUpdate 走 INSERT ... ON CONFLICT DO UPDATE 一条 SQL,复合唯一键
        // (MsgId, EventHandlerName) 保证并发不会插入重复行;之后的 SELECT 一定能看到
        // 当前 (MsgId, EventHandlerName) 对应的那一行(就是 upsert 出来的那行),
        // 不存在"被并发者覆盖查询结果"的问题。补一行 message.Id 回填,让上层
        // LockingHandleAsync(storageId) 能拿到正确的 id。
        await FreeSql.InsertOrUpdate<RelationDbMessageStorageReceivedModel>()
            .SetSource(entity, r => new { r.MsgId, r.EventHandlerName })
            .ExecuteAffrowsAsync(cancellationToken);
        var id = FreeSql.Select<RelationDbMessageStorageReceivedModel>()
            .Where(r => r.MsgId == message.MsgId && r.EventHandlerName == message.EventHandlerName)
            .First(r => r.Id);
        if (id == 0)
            throw new InvalidOperationException(
                $"[EventBus] SaveReceivedAsync: failed to obtain id for msgId={message.MsgId}, handler={message.EventHandlerName}");
        message.Id = id.ToString();
        return message.Id;
    }

    public virtual async Task UpdatePublishedAsync(string storageId, string status, int retryCount,
        DateTimeOffset? expireTime, CancellationToken cancellationToken = default)
    {
        var id = storageId.ParseTo<long>();
        await FreeSql.Update<RelationDbMessageStoragePublishedModel>(id)
            .Set(r => r.Status, status)
            .Set(r => r.RetryCount, retryCount)
            .Set(r => r.ExpireTime, expireTime?.GetLongDate() ?? 0)
            .ExecuteAffrowsAsync(cancellationToken);
    }

    public virtual async Task UpdateReceivedAsync(string storageId, string status, int retryCount,
        DateTimeOffset? expireTime,
        CancellationToken cancellationToken = default)
    {
        var id = storageId.ParseTo<long>();

        await FreeSql.Update<RelationDbMessageStorageReceivedModel>(id)
            .Set(r => r.Status, status)
            .Set(r => r.RetryCount, retryCount)
            .Set(r => r.ExpireTime, expireTime?.GetLongDate() ?? 0)
            .ExecuteAffrowsAsync(cancellationToken);
    }

    public virtual async Task<bool> TryLockPublishedAsync(string storageId, DateTimeOffset lockEndAt,
        CancellationToken cancellationToken)
    {
        var id = storageId.ParseTo<long>();

        var nowLong = DateTimeOffset.Now.GetLongDate();
        return await FreeSql.Update<RelationDbMessageStoragePublishedModel>(id)
            .Where(r => !r.IsLocking || r.LockEnd < nowLong)
            .Set(r => r.IsLocking, true)
            .Set(r => r.LockEnd, lockEndAt.GetLongDate())
            .ExecuteAffrowsAsync(cancellationToken) == 1;
    }

    public virtual async Task<bool> TryLockReceivedAsync(string storageId, DateTimeOffset lockEndAt,
        CancellationToken cancellationToken)
    {
        var id = storageId.ParseTo<long>();

        var nowLong = DateTimeOffset.Now.GetLongDate();
        return await FreeSql.Update<RelationDbMessageStorageReceivedModel>(id)
            .Where(r => !r.IsLocking || r.LockEnd < nowLong)
            .Set(r => r.IsLocking, true)
            .Set(r => r.LockEnd, lockEndAt.GetLongDate())
            .ExecuteAffrowsAsync(cancellationToken) == 1;
    }

    public virtual async Task DeleteExpiresAsync(CancellationToken cancellationToken = default)
    {
        // 分批删除,避免大表上单条 DELETE 锁住表/分区太久。批量大小 1000,可以根据
        // 业务量调整。循环退出条件:某次循环无行被删除(全部过期清理完毕)。
        // FreeSql 的 IDelete<T> 没有稳定的 Limit 支持,所以用"先 select id 列表,再
        // 按 id 删"两步走。
        const int batchSize = 1000;
        var now = DateTimeOffset.Now.GetLongDate();

        while (!cancellationToken.IsCancellationRequested)
        {
            var ids = await FreeSql.Select<RelationDbMessageStoragePublishedModel>()
                .Where(r => r.ExpireTime > 0 && r.ExpireTime < now && r.Status == MessageStatus.Succeeded)
                .Limit(batchSize)
                .ToListAsync(r => r.Id, cancellationToken);
            if (ids.Count == 0) break;
            await FreeSql.Delete<RelationDbMessageStoragePublishedModel>()
                .Where(r => ids.Contains(r.Id))
                .ExecuteAffrowsAsync(cancellationToken);
            if (ids.Count < batchSize) break;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var ids = await FreeSql.Select<RelationDbMessageStorageReceivedModel>()
                .Where(r => r.ExpireTime > 0 && r.ExpireTime < now && r.Status == MessageStatus.Succeeded)
                .Limit(batchSize)
                .ToListAsync(r => r.Id, cancellationToken);
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
        var createTimeLimit = DateTimeOffset.Now.AddSeconds(-delayRetrySecond).GetLongDate();
        var now = DateTimeOffset.Now.GetLongDate();
        // delay 事件只有 DelayAt <= now 才需要重发,否则没到时间。
        return await FreeSql.Select<RelationDbMessageStoragePublishedModel>()
            .Where(r => r.Environment == environment &&
                        ((r.DelayAt == null && r.CreateTime < createTimeLimit) ||
                         (r.DelayAt != null && r.DelayAt <= now)) &&
                        r.RetryCount < maxFailedRetryCount &&
                        (!r.IsLocking || r.LockEnd < now) &&
                        (r.Status == MessageStatus.Scheduled || r.Status == MessageStatus.Failed))
            .Limit(count)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => t.Result.Select(r => r.ToModel()).ToList(), cancellationToken);
    }

    public virtual async Task<List<MessageStorageModel>> GetReceivedMessagesOfNeedRetryAsync(
        int count,
        int delayRetrySecond,
        int maxFailedRetryCount,
        string environment,
        CancellationToken cancellationToken = default)
    {
        var createTimeLimit = DateTimeOffset.Now.AddSeconds(-delayRetrySecond).GetLongDate();
        var now = DateTimeOffset.Now.GetLongDate();
        return await FreeSql.Select<RelationDbMessageStorageReceivedModel>()
            .Where(r => r.Environment == environment &&
                        ((!r.IsDelay && r.CreateTime < createTimeLimit) ||
                         (r.IsDelay && r.DelayAt <= now)) &&
                        r.RetryCount < maxFailedRetryCount &&
                        (!r.IsLocking || r.LockEnd < DateTimeOffset.Now.GetLongDate()) &&
                        (r.Status == MessageStatus.Scheduled || r.Status == MessageStatus.Failed))
            .Limit(count)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => t.Result.Select(r => r.ToModel()).ToList(), cancellationToken);
    }

    public virtual async Task<Dictionary<string, int>> GetPublishedMessageStatusCountsAsync(
        CancellationToken cancellationToken)
    {
        var result = await FreeSql.Select<RelationDbMessageStoragePublishedModel>()
            .GroupBy(r => r.Status)
            .ToListAsync(g => new { g.Key, Count = g.Count() }, cancellationToken);
        return result.ToDictionary(r => r.Key ?? string.Empty, r => r.Count);
    }

    public virtual async Task<Dictionary<string, int>> GetReceivedMessageStatusCountAsync(
        CancellationToken cancellationToken)
    {
        var result = await FreeSql.Select<RelationDbMessageStorageReceivedModel>()
            .GroupBy(r => r.Status)
            .ToListAsync(g => new { g.Key, Count = g.Count() }, cancellationToken);
        return result.ToDictionary(r => r.Key ?? string.Empty, r => r.Count);
    }
}