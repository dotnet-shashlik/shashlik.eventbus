using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
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

    public virtual async Task<List<MessageStorageModel>> SearchPublishedAsync(string environment,
        DateTimeOffset beginTime,
        DateTimeOffset endTime, string? eventName, string? status,
        int skip, int take, CancellationToken cancellationToken)
    {
        var begin = beginTime.UtcTicks;
        var end = endTime.UtcTicks;
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
        var begin = beginTime.UtcTicks;
        var end = endTime.UtcTicks;
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

        // 用 InsertOrUpdate 走 INSERT ... ON CONFLICT DO UPDATE 一条 SQL,复合唯一键
        // (MsgId, EventHandlerName) 保证并发不会插入重复行。
        // 不同方言下 InsertOrUpdate 的副作用不一样:
        // - SqlServer/PostgreSQL/MySQL: 主键是 IsIdentity 自增,upsert 后能立刻查出 Id;
        // - Sqlite: 自增主键是 ROWID,upsert 后 FreeSql 不能保证回填到实体;
        // 因此统一用"先 select 看是否已存在,再 insert 或 update"的两步走,
        // 显式控制 Id 字段写库。这样在所有方言上行为一致。
        var existed = await FreeSql.Select<RelationDbMessageStorageReceivedModel>()
            .Where(r => r.MsgId == message.MsgId && r.EventHandlerName == message.EventHandlerName)
            .FirstAsync(cancellationToken);
        if (existed is null)
        {
            // 插入新行,FreeSql 拿不到 InsertIdentity(部分方言);改用手动累加或 GUID。
            // 这里采用"查 max id + 1"的简单方案(测试场景并发可控,生产场景需要更严谨)。
            // 注意:为了和 SavePublishedAsync 的 IsIdentity 行为保持一致,我们还是
            // 让 FreeSql 来执行插入,只是不依赖它的返回。
            entity.Id = 0;  // IsIdentity 列,让库自己生成
            await FreeSql.Insert(entity).ExecuteAffrowsAsync(cancellationToken);
            var newId = await FreeSql.Select<RelationDbMessageStorageReceivedModel>()
                .Where(r => r.MsgId == message.MsgId && r.EventHandlerName == message.EventHandlerName)
                .FirstAsync(r => r.Id, cancellationToken);
            if (newId == 0)
                throw new InvalidOperationException(
                    $"[EventBus] SaveReceivedAsync: failed to obtain id for msgId={message.MsgId}, handler={message.EventHandlerName}");
            message.Id = newId.ToString();
        }
        else
        {
            // 已存在 -> 更新可变字段,保留 Id
            entity.Id = existed.Id;
            await FreeSql.Update<RelationDbMessageStorageReceivedModel>()
                .SetSource(entity)
                .ExecuteAffrowsAsync(cancellationToken);
            message.Id = existed.Id.ToString();
        }

        return message.Id;
    }

    public virtual async Task UpdatePublishedAsync(string storageId, string status, int retryCount,
        DateTimeOffset? expireTime, CancellationToken cancellationToken = default)
    {
        var id = storageId.ParseTo<long>();
        await FreeSql.Update<RelationDbMessageStoragePublishedModel>(id)
            .Set(r => r.Status, status)
            .Set(r => r.RetryCount, retryCount)
            .Set(r => r.ExpireTimeTicks, expireTime?.UtcTicks)
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
            .Set(r => r.ExpireTimeTicks, expireTime?.UtcTicks)
            .ExecuteAffrowsAsync(cancellationToken);
    }

    public virtual async Task<bool> TryLockPublishedAsync(string storageId, DateTimeOffset lockEndAt,
        CancellationToken cancellationToken)
    {
        var id = storageId.ParseTo<long>();
        var now = DateTimeOffset.UtcNow.UtcTicks;
        return await FreeSql.Update<RelationDbMessageStoragePublishedModel>(id)
            .Where(r => !r.IsLocking || r.LockEndTicks == null || r.LockEndTicks < now)
            .Set(r => r.IsLocking, true)
            .Set(r => r.LockEndTicks, lockEndAt.UtcTicks)
            .ExecuteAffrowsAsync(cancellationToken) == 1;
    }

    public virtual async Task<bool> TryLockReceivedAsync(string storageId, DateTimeOffset lockEndAt,
        CancellationToken cancellationToken)
    {
        var id = storageId.ParseTo<long>();
        var now = DateTimeOffset.UtcNow.UtcTicks;
        return await FreeSql.Update<RelationDbMessageStorageReceivedModel>(id)
            .Where(r => !r.IsLocking || r.LockEndTicks == null || r.LockEndTicks < now)
            .Set(r => r.IsLocking, true)
            .Set(r => r.LockEndTicks, lockEndAt.UtcTicks)
            .ExecuteAffrowsAsync(cancellationToken) == 1;
    }

    public virtual async Task DeleteExpiresAsync(int retryFailedMax, CancellationToken cancellationToken = default)
    {
        // 分批删除,避免大表上单条 DELETE 锁住表/分区太久。批量大小 1000,可以根据
        // 业务量调整。循环退出条件:某次循环无行被删除(全部过期清理完毕)。
        // FreeSql 的 IDelete<T> 没有稳定的 Limit 支持,所以用"先 select id 列表,再
        // 按 id 删"两步走。
        const int batchSize = 1000;
        var now = DateTimeOffset.UtcNow.UtcTicks;

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
        var createTimeLimit = DateTimeOffset.UtcNow.AddSeconds(-delayRetrySecond).UtcTicks;
        var now = DateTimeOffset.UtcNow.UtcTicks;
        // 普通消息
        var normalQuery = FreeSql.Select<RelationDbMessageStoragePublishedModel>()
            .Where(r => r.Environment == environment &&
                        r.Status.In(MessageStatus.Scheduled, MessageStatus.Failed) &&
                        r.IsDelay == false &&
                        r.CreateTimeTicks < createTimeLimit &&
                        r.RetryCount < maxFailedRetryCount &&
                        (!r.IsLocking || r.LockEndTicks == null || r.LockEndTicks < now))
            .OrderByDescending(r => r.CreateTimeTicks);

        // 延迟消息
        var delayQuery = FreeSql.Select<RelationDbMessageStoragePublishedModel>()
            .Where(r => r.Environment == environment &&
                        r.Status.In(MessageStatus.Scheduled, MessageStatus.Failed) &&
                        r.IsDelay == true &&
                        r.DelayAtTicks <= now &&
                        r.RetryCount < maxFailedRetryCount &&
                        (!r.IsLocking || r.LockEndTicks == null || r.LockEndTicks < now))
            .OrderByDescending(r => r.CreateTimeTicks);

        // 合并并排序、限制数量
        return await normalQuery
            .UnionAll(delayQuery)
            .OrderByDescending(r => r.CreateTimeTicks)
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
        var createTimeLimit = DateTimeOffset.UtcNow.AddSeconds(-delayRetrySecond).UtcTicks;
        var now = DateTimeOffset.UtcNow.UtcTicks;
        // 普通消息
        var normalQuery = FreeSql.Select<RelationDbMessageStorageReceivedModel>()
            .Where(r => r.Environment == environment &&
                        r.Status.In(MessageStatus.Scheduled, MessageStatus.Failed) &&
                        r.IsDelay == false &&
                        r.CreateTimeTicks < createTimeLimit &&
                        r.RetryCount < maxFailedRetryCount &&
                        (!r.IsLocking || r.LockEndTicks == null || r.LockEndTicks < now))
            .OrderByDescending(r => r.CreateTimeTicks);

        // 延迟消息
        var delayQuery = FreeSql.Select<RelationDbMessageStorageReceivedModel>()
            .Where(r => r.Environment == environment &&
                        r.Status.In(MessageStatus.Scheduled, MessageStatus.Failed) &&
                        r.IsDelay == true &&
                        r.DelayAtTicks <= now &&
                        r.RetryCount < maxFailedRetryCount &&
                        (!r.IsLocking || r.LockEndTicks == null || r.LockEndTicks < now))
            .OrderByDescending(r => r.CreateTimeTicks);

        // 合并并排序、限制数量
        return await normalQuery
            .UnionAll(delayQuery)
            .OrderByDescending(r => r.CreateTimeTicks)
            .Limit(count)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => t.Result.Select(r => r.ToModel()).ToList(), cancellationToken);
    }

    public virtual async Task<Dictionary<string, int>> GetPublishedMessageStatusCountsAsync(
        string environment, DateTimeOffset beginTime, DateTimeOffset endTime, CancellationToken cancellationToken)
    {
        var begin = beginTime.UtcTicks;
        var end = endTime.UtcTicks;
        var result = await FreeSql.Select<RelationDbMessageStoragePublishedModel>()
            .Where(r => r.CreateTimeTicks >= begin && r.CreateTimeTicks <= end && r.Environment == environment)
            .GroupBy(r => r.Status)
            .ToListAsync(g => new { g.Key, Count = g.Count() }, cancellationToken);
        return result.ToDictionary(r => r.Key ?? string.Empty, r => r.Count);
    }

    public virtual async Task<Dictionary<string, int>> GetReceivedMessageStatusCountAsync(
        string environment, DateTimeOffset beginTime, DateTimeOffset endTime,
        CancellationToken cancellationToken)
    {
        var begin = beginTime.UtcTicks;
        var end = endTime.UtcTicks;
        var result = await FreeSql.Select<RelationDbMessageStorageReceivedModel>()
            .Where(r => r.CreateTimeTicks >= begin && r.CreateTimeTicks <= end && r.Environment == environment)
            .GroupBy(r => r.Status)
            .ToListAsync(g => new { g.Key, Count = g.Count() }, cancellationToken);
        return result.ToDictionary(r => r.Key ?? string.Empty, r => r.Count);
    }
}