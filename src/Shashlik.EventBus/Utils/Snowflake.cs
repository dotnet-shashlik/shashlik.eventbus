using System;
using System.Threading;

namespace Shashlik.EventBus.Utils;

/// <summary>
/// 雪花算法ID生成器.
/// <para>
/// 64 bit 布局: 1 bit 符号(始终为0) | 41 bit 时间戳(毫秒) | 10 bit WorkerId | 12 bit 自增序号.
/// </para>
/// <para>
/// 单个实例单毫秒最多可生成 4096 个ID, 最多支持 1024 个 Worker.
/// </para>
/// <para>
/// 由使用方自行持有实例(通常作为静态单例), workerId 通过构造函数传入.
/// </para>
/// </summary>
public class Snowflake
{
    public const int WorkerIdBits = 10;
    public const int SequenceBits = 12;

    public const long MaxWorkerId = (1L << WorkerIdBits) - 1;  // 1023
    public const long MaxSequence = (1L << SequenceBits) - 1;  // 4095

    private const int WorkerIdShift = SequenceBits;                  // 12
    private const int TimestampShift = SequenceBits + WorkerIdBits;  // 22

    private static readonly DateTimeOffset Epoch =
        new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly object _lock = new();
    private readonly long _workerId;
    private long _lastTimestamp = -1L;
    private long _sequence;

    /// <summary>
    /// 使用 workerId 构造一个雪花ID生成器实例, 范围 [0, <see cref="MaxWorkerId"/>].
    /// </summary>
    /// <param name="workerId">worker id (0 ~ 1023)</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Snowflake(ushort workerId)
    {
        if (workerId > MaxWorkerId)
            throw new ArgumentOutOfRangeException(nameof(workerId),
                $"WorkerId must be in [0, {MaxWorkerId}]");

        _workerId = workerId;
    }

    /// <summary>
    /// 当前实例的 WorkerId.
    /// </summary>
    public long WorkerId => _workerId;

    /// <summary>
    /// 生成下一个唯一ID.
    /// </summary>
    /// <returns>long 类型的雪花ID</returns>
    /// <exception cref="InvalidOperationException">系统时钟回退时抛出</exception>
    public long NextId()
    {
        lock (_lock)
        {
            var timestamp = CurrentTimestamp();

            if (timestamp < _lastTimestamp)
                throw new InvalidOperationException(
                    $"Clock moved backwards. Refusing to generate id for {_lastTimestamp - timestamp} milliseconds.");

            if (timestamp == _lastTimestamp)
            {
                _sequence = (_sequence + 1) & MaxSequence;
                if (_sequence == 0)
                    timestamp = WaitNextMillis(_lastTimestamp);
            }
            else
            {
                _sequence = 0L;
            }

            _lastTimestamp = timestamp;

            return ((timestamp - Epoch.ToUnixTimeMilliseconds()) << TimestampShift)
                 | (_workerId << WorkerIdShift)
                 | _sequence;
        }
    }

    private static long CurrentTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    private static long WaitNextMillis(long lastTimestamp)
    {
        SpinWait.SpinUntil(() => CurrentTimestamp() > lastTimestamp);
        return CurrentTimestamp();
    }
}
