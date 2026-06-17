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
    public static MessageStorageModel ToModel(this RelationDbMessageStoragePublishedModel entity)
    {
        return new MessageStorageModel
        {
            Id = entity.Id,
            MsgId = entity.MsgId,
            Environment = entity.Environment,
            EventName = entity.EventName,
            EventBody = entity.EventBody,
            CreateTime = entity.CreateTimeTicks.LongToDateTimeOffset()!.Value,
            DelayAt = entity.DelayAtTicks.LongToDateTimeOffset(),
            ExpireTime = entity.ExpireTimeTicks.LongToDateTimeOffset(),
            EventItems = entity.EventItems,
            RetryCount = entity.RetryCount,
            Status = entity.Status,
            IsLocking = entity.IsLocking,
            LockEnd = entity.LockEndTicks.LongToDateTimeOffset()
        };
    }

    public static MessageStorageModel ToModel(this RelationDbMessageStorageReceivedModel entity)
    {
        return new MessageStorageModel
        {
            Id = entity.Id,
            MsgId = entity.MsgId,
            Environment = entity.Environment,
            EventName = entity.EventName,
            EventHandlerName = entity.EventHandlerName,
            EventBody = entity.EventBody,
            CreateTime = entity.CreateTimeTicks.LongToDateTimeOffset()!.Value,
            DelayAt = entity.DelayAtTicks.LongToDateTimeOffset(),
            ExpireTime = entity.ExpireTimeTicks.LongToDateTimeOffset(),
            EventItems = entity.EventItems,
            RetryCount = entity.RetryCount,
            Status = entity.Status,
            IsLocking = entity.IsLocking,
            LockEnd = entity.LockEndTicks.LongToDateTimeOffset()
        };
    }

    public static RelationDbMessageStoragePublishedModel ToPublishedSaveObject(this MessageStorageModel model)
    {
        return new RelationDbMessageStoragePublishedModel
        {
            Id = model.Id,
            MsgId = model.MsgId,
            Environment = model.Environment,
            EventName = model.EventName,
            EventBody = model.EventBody,
            CreateTimeTicks = model.CreateTime.GetLongDate(),
            IsDelay = model.DelayAt.HasValue,
            DelayAtTicks = model.DelayAt.GetLongDate(),
            ExpireTimeTicks = model.ExpireTime.GetLongDate(),
            EventItems = model.EventItems,
            RetryCount = model.RetryCount,
            Status = model.Status,
            IsLocking = model.IsLocking,
            LockEndTicks = model.LockEnd.GetLongDate()
        };
    }

    public static RelationDbMessageStorageReceivedModel ToReceivedSaveObject(this MessageStorageModel model)
    {
        return new RelationDbMessageStorageReceivedModel
        {
            Id = model.Id,
            MsgId = model.MsgId,
            Environment = model.Environment,
            EventName = model.EventName,
            EventHandlerName = model.EventHandlerName,
            EventBody = model.EventBody,
            CreateTimeTicks = model.CreateTime.GetLongDate(),
            IsDelay = model.DelayAt.HasValue,
            DelayAtTicks = model.DelayAt.GetLongDate(),
            ExpireTimeTicks = model.ExpireTime.GetLongDate(),
            EventItems = model.EventItems,
            RetryCount = model.RetryCount,
            Status = model.Status,
            IsLocking = model.IsLocking,
            LockEndTicks = model.LockEnd.GetLongDate()
        };
    }
}