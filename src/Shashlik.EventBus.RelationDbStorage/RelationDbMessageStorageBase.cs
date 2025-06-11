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
/// 提供关系型数据库的一些默认方法
/// </summary>
public abstract class RelationDbMessageStorageBase : IMessageStorage
{
    protected RelationDbMessageStorageBase(IFreeSqlFactory freeSqlFactory)
    {
        FreeSql = freeSqlFactory.Instance();
    }

    protected IFreeSql FreeSql { get; }

    private MessageStorageModel ToModel(RelationDbMessageStoragePublishedModel model)
    {
        return new MessageStorageModel
        {
            Id = model.Id.ToString(),
            MsgId = model.MsgId,
            Environment = model.Environment,
            EventName = model.EventName,
            EventBody = model.EventBody,
            CreateTime = model.CreateTime.LongToDateTimeOffset()!.Value,
            DelayAt = model.DelayAt.LongToDateTimeOffset(),
            ExpireTime = model.ExpireTime.LongToDateTimeOffset(),
            EventItems = model.EventItems,
            RetryCount = model.RetryCount,
            Status = model.Status,
            IsLocking = model.IsLocking,
            LockEnd = model.LockEnd.LongToDateTimeOffset()
        };
    }

    private MessageStorageModel ToModel(RelationDbMessageStorageReceivedModel model)
    {
        return new MessageStorageModel
        {
            Id = model.Id.ToString(),
            MsgId = model.MsgId,
            Environment = model.Environment,
            EventName = model.EventName,
            EventHandlerName = model.EventHandlerName,
            EventBody = model.EventBody,
            CreateTime = model.CreateTime.LongToDateTimeOffset()!.Value,
            DelayAt = model.DelayAt.LongToDateTimeOffset(),
            ExpireTime = model.ExpireTime.LongToDateTimeOffset(),
            EventItems = model.EventItems,
            RetryCount = model.RetryCount,
            Status = model.Status,
            IsLocking = model.IsLocking,
            LockEnd = model.LockEnd.LongToDateTimeOffset()
        };
    }

    public RelationDbMessageStoragePublishedModel ToPublishedSaveObject(MessageStorageModel model)
    {
        return new RelationDbMessageStoragePublishedModel
        {
            MsgId = model.MsgId,
            Environment = model.Environment,
            EventName = model.EventName,
            EventBody = model.EventBody,
            CreateTime = model.CreateTime.GetLongDate(),
            DelayAt = model.DelayAt?.GetLongDate() ?? 0,
            ExpireTime = model.ExpireTime?.GetLongDate() ?? 0,
            EventItems = model.EventItems,
            RetryCount = model.RetryCount,
            Status = model.Status,
            IsLocking = model.IsLocking,
            LockEnd = model.LockEnd?.GetLongDate() ?? 0,
        };
    }

    public RelationDbMessageStorageReceivedModel ToReceivedSaveObject(MessageStorageModel model)
    {
        return new RelationDbMessageStorageReceivedModel
        {
            MsgId = model.MsgId,
            Environment = model.Environment,
            EventName = model.EventName,
            EventHandlerName = model.EventHandlerName,
            EventBody = model.EventBody,
            CreateTime = model.CreateTime.GetLongDate(),
            IsDelay = model.DelayAt.HasValue,
            DelayAt = model.DelayAt?.GetLongDate() ?? 0,
            ExpireTime = model.ExpireTime?.GetLongDate() ?? 0,
            EventItems = model.EventItems,
            RetryCount = model.RetryCount,
            Status = model.Status,
            IsLocking = model.IsLocking,
            LockEnd = model.LockEnd?.GetLongDate() ?? 0
        };
    }

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
        if (entity is null)
            return null;
        return ToModel(entity);
    }

    public virtual async Task<MessageStorageModel?> FindPublishedByIdAsync(string storageId,
        CancellationToken cancellationToken)
    {
        var id = storageId.ParseTo<long>();
        var entity = await FreeSql.Select<RelationDbMessageStoragePublishedModel>(id)
            .FirstAsync(cancellationToken);
        if (entity is null)
            return null;
        return ToModel(entity);
    }

    public virtual async Task<MessageStorageModel?> FindReceivedByMsgIdAsync(string msgId,
        EventHandlerDescriptor eventHandlerDescriptor,
        CancellationToken cancellationToken = default)
    {
        var entity = await FreeSql.Select<RelationDbMessageStorageReceivedModel>()
            .Where(r => r.MsgId == msgId && r.EventHandlerName == eventHandlerDescriptor.EventHandlerName)
            .FirstAsync(cancellationToken);
        if (entity is null)
            return null;
        return ToModel(entity);
    }

    public virtual async Task<MessageStorageModel?> FindReceivedByIdAsync(string storageId,
        CancellationToken cancellationToken)
    {
        var id = storageId.ParseTo<long>();
        var entity = await FreeSql.Select<RelationDbMessageStorageReceivedModel>(id)
            .FirstAsync(cancellationToken);
        if (entity is null)
            return null;
        return ToModel(entity);
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
        return result.Select(ToModel).ToList();
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
        return result.Select(ToModel).ToList();
    }

    public virtual async Task<string> SavePublishedAsync(MessageStorageModel message,
        ITransactionContext? transactionContext,
        CancellationToken cancellationToken = default)
    {
        var entity = ToPublishedSaveObject(message);
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
        var entity = ToReceivedSaveObject(message);

        await FreeSql.InsertOrUpdate<RelationDbMessageStorageReceivedModel>()
            .SetSource(entity, r => new { r.MsgId, r.EventHandlerName })
            .ExecuteAffrowsAsync(cancellationToken);
        var id = FreeSql.Select<RelationDbMessageStorageReceivedModel>()
            .Where(r => r.MsgId == message.MsgId && r.EventHandlerName == message.EventHandlerName)
            .First(r => r.Id);
        message.Id = id.ToString();
        return id.ToString();
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
        var now = DateTimeOffset.Now.GetLongDate();
        await FreeSql.Delete<RelationDbMessageStoragePublishedModel>()
            .Where(r => r.ExpireTime > 0 && r.ExpireTime < now && r.Status == MessageStatus.Succeeded)
            .ExecuteAffrowsAsync(cancellationToken);

        await FreeSql.Delete<RelationDbMessageStorageReceivedModel>()
            .Where(r => r.ExpireTime > 0 && r.ExpireTime < now && r.Status == MessageStatus.Succeeded)
            .ExecuteAffrowsAsync(cancellationToken);
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
        return await FreeSql.Select<RelationDbMessageStoragePublishedModel>()
            .Where(r => r.Environment == environment &&
                        r.CreateTime < createTimeLimit &&
                        r.RetryCount < maxFailedRetryCount &&
                        (!r.IsLocking || r.LockEnd < now) &&
                        (r.Status == MessageStatus.Scheduled || r.Status == MessageStatus.Failed))
            .Limit(count)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => t.Result.Select(ToModel).ToList(), cancellationToken);
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
            .ContinueWith(t => t.Result.Select(ToModel).ToList(), cancellationToken);
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