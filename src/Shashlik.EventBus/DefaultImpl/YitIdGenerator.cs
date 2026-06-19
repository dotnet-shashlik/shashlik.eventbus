using System;
using System.Security.Cryptography;
using Yitter.IdGenerator;

namespace Shashlik.EventBus.DefaultImpl;

/// <summary>
/// 主id生成器, 默认YitId, 从环境变量WORKER_ID(0~1023)中读取, 否则使用随机值
/// </summary>
public class YitIdGenerator : IIdGenerator
{
    public YitIdGenerator()
    {
        YitIdHelper.SetIdGenerator(new IdGeneratorOptions
        {
            WorkerId = GetWorkerId(),
            WorkerIdBitLength = 10
        });
    }

    public virtual ushort GetWorkerId()
    {
        var workerIdStr = Environment.GetEnvironmentVariable("WORKER_ID");
        if (ushort.TryParse(workerIdStr, out var workerId))
        {
            if (workerId >= 1024)
                throw new ArgumentOutOfRangeException(nameof(workerId));

            YitIdHelper.SetIdGenerator(new IdGeneratorOptions
            {
                WorkerId = workerId,
                WorkerIdBitLength = 10
            });
        }
        else
            workerId = (ushort)RandomNumberGenerator.GetInt32(0, 1024);

        return workerId;
    }


    public virtual long NextId()
    {
        return YitIdHelper.NextId();
    }
}