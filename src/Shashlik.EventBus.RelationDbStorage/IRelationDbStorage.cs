using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.RelationDbStorage;

/// <summary>
/// 提供关系型数据库的一些默认方法
/// </summary>
public interface IRelationDbStorage : IMessageStorage
{
    IDbConnection CreateConnection();

    private MessageStorageModel ToModel(dynamic model)
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

    public async Task<MessageStorageModel?> QueryOneModelAsync(string sql, object? ps,
        CancellationToken cancellationToken = default)
    {
        using var dbConnection = CreateConnection();
        dbConnection.Open();
        var model = await dbConnection.QueryFirstOrDefaultAsync(sql, ps).ConfigureAwait(false);
        return model is null ? null : (MessageStorageModel?)ToModel(model);
    }

    public async Task<List<MessageStorageModel>> QueryModelAsync(string sql, object? ps,
        CancellationToken cancellationToken = default)
    {
        using var dbConnection = CreateConnection();
        dbConnection.Open();
        var list = await dbConnection.QueryAsync(sql, ps).ConfigureAwait(false);
        return list.Select(ToModel).ToList();
    }

    public async Task<TRes> ScalarAsync<TRes>(string sql, object? ps,
        CancellationToken cancellationToken = default)
    {
        using var dbConnection = CreateConnection();
        dbConnection.Open();
        return await dbConnection.ExecuteScalarAsync<TRes>(sql, ps).ConfigureAwait(false);
    }

    public async Task<int> NonQueryAsync(string sql, object? ps,
        CancellationToken cancellationToken = default)
    {
        using var dbConnection = CreateConnection();
        dbConnection.Open();
        return await dbConnection.ExecuteAsync(sql, ps).ConfigureAwait(false);
    }

    public async Task<TRes> ScalarAsync<TRes>(ITransactionContext? transactionContext, string sql,
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

    public object ToSaveObject(MessageStorageModel model)
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
            IsLocking = model.IsLocking ? 1 : 0,
            LockEnd = model.LockEnd?.GetLongDate() ?? 0,
            IsDelay = model.DelayAt.HasValue ? 1 : 0
        };
    }
}