using Shashlik.EventBus.Utils;

// ReSharper disable CheckNamespace

namespace Shashlik.EventBus.RelationDbStorage;

/// <summary>
/// <see cref="MessageStorageModel"/> 与 FreeSql 实体模型
/// (<see cref="RelationDbMessageStoragePublishedModel"/>,
///  <see cref="RelationDbMessageStorageReceivedModel"/>) 之间的相互转换。
/// 集中放在这里避免在抽象类里散落重复的 ToModel / ToSaveObject。
/// </summary>
internal static class MessageStorageModelMapping
{
    public static MessageStorageModel ToModel(this RelationDbMessageStoragePublishedModel entity)
    {
        return new MessageStorageModel
        {
            Id = entity.Id.ToString(),
            MsgId = entity.MsgId,
            Environment = entity.Environment,
            EventName = entity.EventName,
            EventBody = entity.EventBody,
            CreateTime = entity.CreateTime.LongToDateTimeOffset()!.Value,
            DelayAt = entity.DelayAt?.LongToDateTimeOffset(),
            ExpireTime = entity.ExpireTime?.LongToDateTimeOffset(),
            EventItems = entity.EventItems,
            RetryCount = entity.RetryCount,
            Status = entity.Status,
            IsLocking = entity.IsLocking,
            LockEnd = entity.LockEnd?.LongToDateTimeOffset()
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
            CreateTime = entity.CreateTime.LongToDateTimeOffset()!.Value,
            DelayAt = entity.DelayAt?.LongToDateTimeOffset(),
            ExpireTime = entity.ExpireTime?.LongToDateTimeOffset(),
            EventItems = entity.EventItems,
            RetryCount = entity.RetryCount,
            Status = entity.Status,
            IsLocking = entity.IsLocking,
            LockEnd = entity.LockEnd?.LongToDateTimeOffset()
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
            CreateTime = model.CreateTime.GetLongDate(),
            DelayAt = model.DelayAt?.GetLongDate(),
            ExpireTime = model.ExpireTime?.GetLongDate(),
            EventItems = model.EventItems,
            RetryCount = model.RetryCount,
            Status = model.Status,
            IsLocking = model.IsLocking,
            LockEnd = model.LockEnd?.GetLongDate(),
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
            CreateTime = model.CreateTime.GetLongDate(),
            IsDelay = model.DelayAt.HasValue,
            DelayAt = model.DelayAt?.GetLongDate(),
            ExpireTime = model.ExpireTime?.GetLongDate(),
            EventItems = model.EventItems,
            RetryCount = model.RetryCount,
            Status = model.Status,
            IsLocking = model.IsLocking,
            LockEnd = model.LockEnd?.GetLongDate()
        };
    }
}
