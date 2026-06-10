using System;
using Shashlik.EventBus.Utils;

// ReSharper disable CheckNamespace

namespace Shashlik.EventBus.RelationDbStorage;

/// <summary>
/// <see cref="MessageStorageModel"/> 与 FreeSql 实体模型
/// (<see cref="RelationDbMessageStoragePublishedModel"/>,
///  <see cref="RelationDbMessageStorageReceivedModel"/>) 之间的相互转换。
/// 集中放在这里避免在抽象类里散落重复的 ToModel / ToSaveObject。
/// 实体内部统一用 UTC ticks (long) 持久化(避开 FreeSql.Provider.Sqlite 对 DateTime 字段做
/// ToLocalTime 转换、以及 CodeFirst 静默丢掉 DateTimeOffset 列两个问题),
/// 与上层 DTO <see cref="DateTimeOffset"/> 互转。MySQL/PostgreSQL/SqlServer 这些
/// provider 对 DateTime 友好,但 ticks 同样可移植,所以统一在持久化层做转换。
/// </summary>
internal static class MessageStorageModelMapping
{
    private static DateTimeOffset TicksToDto(long ticks) =>
        new DateTimeOffset(ticks, TimeSpan.Zero);

    private static DateTimeOffset? TicksToDto(long? ticks) =>
        ticks.HasValue ? (DateTimeOffset?)TicksToDto(ticks.Value) : null;

    private static long TicksToDb(DateTimeOffset dt) =>
        dt.UtcTicks;

    private static long? TicksToDb(DateTimeOffset? dt) =>
        dt?.UtcTicks;

    public static MessageStorageModel ToModel(this RelationDbMessageStoragePublishedModel entity)
    {
        return new MessageStorageModel
        {
            Id = entity.Id.ToString(),
            MsgId = entity.MsgId,
            Environment = entity.Environment,
            EventName = entity.EventName,
            EventBody = entity.EventBody,
            CreateTime = TicksToDto(entity.CreateTimeTicks),
            DelayAt = TicksToDto(entity.DelayAtTicks),
            ExpireTime = TicksToDto(entity.ExpireTimeTicks),
            EventItems = entity.EventItems,
            RetryCount = entity.RetryCount,
            Status = entity.Status,
            IsLocking = entity.IsLocking,
            LockEnd = TicksToDto(entity.LockEndTicks)
        };
    }

    public static MessageStorageModel ToModel(this RelationDbMessageStorageReceivedModel entity)
    {
        return new MessageStorageModel
        {
            Id = entity.Id.ToString(),
            MsgId = entity.MsgId,
            Environment = entity.Environment,
            EventName = entity.EventName,
            EventHandlerName = entity.EventHandlerName,
            EventBody = entity.EventBody,
            CreateTime = TicksToDto(entity.CreateTimeTicks),
            DelayAt = TicksToDto(entity.DelayAtTicks),
            ExpireTime = TicksToDto(entity.ExpireTimeTicks),
            EventItems = entity.EventItems,
            RetryCount = entity.RetryCount,
            Status = entity.Status,
            IsLocking = entity.IsLocking,
            LockEnd = TicksToDto(entity.LockEndTicks)
        };
    }

    public static RelationDbMessageStoragePublishedModel ToPublishedSaveObject(this MessageStorageModel model)
    {
        return new RelationDbMessageStoragePublishedModel
        {
            MsgId = model.MsgId,
            Environment = model.Environment,
            EventName = model.EventName,
            EventBody = model.EventBody,
            CreateTimeTicks = TicksToDb(model.CreateTime),
            IsDelay = model.DelayAt.HasValue,
            DelayAtTicks = TicksToDb(model.DelayAt),
            ExpireTimeTicks = TicksToDb(model.ExpireTime),
            EventItems = model.EventItems,
            RetryCount = model.RetryCount,
            Status = model.Status,
            IsLocking = model.IsLocking,
            LockEndTicks = TicksToDb(model.LockEnd)
        };
    }

    public static RelationDbMessageStorageReceivedModel ToReceivedSaveObject(this MessageStorageModel model)
    {
        return new RelationDbMessageStorageReceivedModel
        {
            MsgId = model.MsgId,
            Environment = model.Environment,
            EventName = model.EventName,
            EventHandlerName = model.EventHandlerName,
            EventBody = model.EventBody,
            CreateTimeTicks = TicksToDb(model.CreateTime),
            IsDelay = model.DelayAt.HasValue,
            DelayAtTicks = TicksToDb(model.DelayAt),
            ExpireTimeTicks = TicksToDb(model.ExpireTime),
            EventItems = model.EventItems,
            RetryCount = model.RetryCount,
            Status = model.Status,
            IsLocking = model.IsLocking,
            LockEndTicks = TicksToDb(model.LockEnd)
        };
    }
}