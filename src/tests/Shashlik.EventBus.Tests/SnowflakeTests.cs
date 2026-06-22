using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Shashlik.EventBus.Utils;
using Shouldly;
using Xunit;

namespace Shashlik.EventBus.Tests;

/// <summary>
/// 雪花算法工具类单元测试.
/// </summary>
[Collection("Shashlik.EventBus.Tests")]
public class SnowflakeTests
{
    private const int SequenceBits = 12;
    private const int WorkerIdBits = 10;
    private const int TimestampShift = SequenceBits + WorkerIdBits; // 22
    private const int WorkerIdShift = SequenceBits;                // 12
    private const long MaxWorkerId = (1L << WorkerIdBits) - 1;      // 1023
    private const long WorkerIdMask = MaxWorkerId << WorkerIdShift;

    [Fact]
    public void Constructor_OutOfRangeWorkerId_ShouldThrow()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new Snowflake((ushort)1024));
        Should.Throw<ArgumentOutOfRangeException>(() => new Snowflake(ushort.MaxValue));
    }

    [Fact]
    public void Constructor_ShouldStoreWorkerId()
    {
        new Snowflake(0).WorkerId.ShouldBe(0);
        new Snowflake((ushort)MaxWorkerId).WorkerId.ShouldBe(MaxWorkerId);
        new Snowflake((ushort)123).WorkerId.ShouldBe(123);
    }

    [Fact]
    public void NextId_ShouldReturnPositiveId()
    {
        var id = new Snowflake(0).NextId();
        id.ShouldBeGreaterThan(0L);
    }

    [Fact]
    public void NextId_SignBit_ShouldAlwaysBeZero()
    {
        var snowflake = new Snowflake(0);
        for (var i = 0; i < 1000; i++)
        {
            var id = snowflake.NextId();
            (id < 0L).ShouldBeFalse();
            ((id >> 63) & 1).ShouldBe(0L);
        }
    }

    [Fact]
    public void NextId_ShouldEmbedConfiguredWorkerId()
    {
        const ushort wid = 7;
        var snowflake = new Snowflake(wid);
        for (var i = 0; i < 100; i++)
        {
            var id = snowflake.NextId();
            var embedded = (id & WorkerIdMask) >> WorkerIdShift;
            embedded.ShouldBe(wid);
        }
    }

    [Fact]
    public void NextId_Sequential_SameTimestamp_ShouldIncreaseSequence()
    {
        var snowflake = new Snowflake(0);
        var ids = new long[10];
        for (var i = 0; i < ids.Length; i++)
            ids[i] = snowflake.NextId();

        for (var i = 1; i < ids.Length; i++)
            (ids[i] > ids[i - 1]).ShouldBeTrue();
    }

    // ---- 相同 workerId 实例: 单实例大量调用, IDs 不应重复 ----

    [Fact]
    public void NextId_SameInstance_Sequential_Batch_ShouldBeUnique()
    {
        const int count = 10_000;
        var snowflake = new Snowflake(0);
        var ids = new long[count];
        for (var i = 0; i < count; i++)
            ids[i] = snowflake.NextId();

        ids.Distinct().Count().ShouldBe(count);
    }

    [Fact]
    public async Task NextId_SameInstance_Concurrent_ShouldProduceUniqueIds()
    {
        const int threadCount = 16;
        const int perThread = 5_000;

        var snowflake = new Snowflake(0);
        var allIds = new ConcurrentBag<long>();

        var tasks = Enumerable.Range(0, threadCount)
            .Select(_ => Task.Run(() =>
            {
                for (var i = 0; i < perThread; i++)
                    allIds.Add(snowflake.NextId());
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        allIds.Count.ShouldBe(threadCount * perThread);
        allIds.Distinct().Count().ShouldBe(threadCount * perThread);
    }

    // ---- 不同 workerId: 多实例不同 workerId 并发, IDs 全局不应重复 ----

    [Fact]
    public async Task NextId_DifferentInstances_DifferentWorkerIds_Concurrent_ShouldProduceUniqueIds()
    {
        const int instanceCount = 8;
        const int perInstance = 5_000;

        // 使用 8 个不同的 workerId
        var workerIds = new ushort[] { 0, 1, 2, 3, 100, 500, 800, 1023 };
        workerIds.Length.ShouldBe(instanceCount);

        var instances = workerIds.Select(w => new Snowflake(w)).ToArray();

        var allIds = new ConcurrentBag<long>();
        var tasks = instances
            .Select(instance => Task.Run(() =>
            {
                for (var i = 0; i < perInstance; i++)
                    allIds.Add(instance.NextId());
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        allIds.Count.ShouldBe(instanceCount * perInstance);
        allIds.Distinct().Count().ShouldBe(instanceCount * perInstance);
    }

    [Fact]
    public void NextId_DifferentInstances_DifferentWorkerIds_EmbedCorrectWorkerId()
    {
        var workerIds = new ushort[] { 0, 1, 2, 3, 100, 500, 800, 1023 };
        var instances = workerIds.Select(w => new Snowflake(w)).ToArray();

        // 每个实例生成一些 ID, 提取 workerId 字段应匹配
        for (var k = 0; k < instances.Length; k++)
        {
            for (var i = 0; i < 50; i++)
            {
                var id = instances[k].NextId();
                var embedded = (id & WorkerIdMask) >> WorkerIdShift;
                embedded.ShouldBe(workerIds[k]);
            }
        }
    }

    [Fact]
    public void NextId_TimestampEmbedded_ShouldBeReasonable()
    {
        var snowflake = new Snowflake(0);
        var beforeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var id = snowflake.NextId();
        var afterMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var tsMs = id >> TimestampShift;
        var epochMs = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        tsMs.ShouldBeGreaterThan(beforeMs - epochMs - 1);
        tsMs.ShouldBeLessThan(afterMs - epochMs + 1);
    }
}
