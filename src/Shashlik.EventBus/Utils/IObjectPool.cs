using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shashlik.EventBus.Utils;

/// <summary>
/// 池对象的"归还句柄"。调用方 <c>await using var lease = pool.RentAsync()</c> 后,
/// 离开作用域时自动调用 <see cref="DisposeAsync"/> 把对象归还到池。
/// </summary>
public interface IPoolLease<T> : IAsyncDisposable where T : class
{
    /// <summary>
    /// 租借到的对象,可能为 null(创建失败时)
    /// </summary>
    T? Value { get; }

    /// <summary>
    /// 租借的当前是否仍有效。归还时会读取此标志,无效的对象被直接丢弃(不归还到池)。
    /// </summary>
    bool IsValid { get; set; }

    /// <summary>
    /// 池分配时打的 key(用于诊断/泄漏追踪)。
    /// </summary>
    string PoolKey { get; }
}

/// <summary>
/// 池策略,负责新对象的创建和复用前的校验。
/// </summary>
public interface IObjectPoolPolicy<T> where T : class
{
    /// <summary>
    /// 创建一个新对象。一般用于:首次租借时 / 复用校验失败时。
    /// </summary>
    ValueTask<T> CreateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 归还时校验对象是否仍可复用(例如 channel.IsClosed == false)。
    /// 返回 false 时,池直接丢弃,不再加入空闲队列。
    /// </summary>
    bool TryReuse(T item);
}

/// <summary>
/// 异步对象池。
/// <para>设计要点:</para>
/// <list type="bullet">
///   <item>使用 <see cref="SemaphoreSlim"/> 串行化创建,避免大量并发首次租借时打爆下游(创建 IChannel / IProducer 都是网络 RTT)。</item>
///   <item>空闲对象用 <see cref="System.Collections.Concurrent.ConcurrentBag{T}"/> 装,无锁归还/取出。</item>
///   <item>总实例数(含已租出 + 空闲)硬上限 = <c>maxSize</c>,超过时 <see cref="RentAsync"/> 阻塞等待,而不是无限扩张。</item>
/// </list>
/// </summary>
public interface IObjectPool<T> where T : class
{
    /// <summary>
    /// 当前池子名,用于日志/诊断。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 池容量(总实例数上限)。
    /// </summary>
    int MaxSize { get; }

    /// <summary>
    /// 借一个对象。如果池达到上限,会等待直到有对象归还;cancellationToken 可取消等待。
    /// </summary>
    ValueTask<IPoolLease<T>> RentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 把对象归还到池,内部实现必须线程安全,通常由 <see cref="IPoolLease{T}"/> 的 DisposeAsync 调用。
    /// </summary>
    void Return(T item, bool isValid);

    /// <summary>
    /// 已创建的总实例数(含已租出)。用于诊断/指标。
    /// </summary>
    int Count { get; }
}

/// <summary>
/// 对象池提供器,通常在 DI 容器里以单例注册。各 MQ/DB 连接池通过此接口创建。
/// </summary>
public interface IObjectPoolProvider
{
    IObjectPool<T> Create<T>(string name, IObjectPoolPolicy<T> policy, int maxSize)
        where T : class;
}
