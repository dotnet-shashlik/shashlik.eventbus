using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Shashlik.EventBus.Utils;

/// <summary>
/// <see cref="IObjectPool{T}"/> 的默认实现。
/// <para>线程安全策略:</para>
/// <list type="bullet">
///   <item><see cref="_totalCount"/> 用 <see cref="Interlocked.Increment"/> / <see cref="Interlocked.Decrement"/> 维护,避免锁。</item>
///   <item>空闲对象用 <see cref="ConcurrentBag{T}"/> 装,无锁 push/try-take。</item>
///   <item>用 <see cref="SemaphoreSlim"/>(initialCount=<see cref="MaxSize"/>) 限制总实例数:每借出一个则 Wait 一次,每归还一个则 Release 一次。满载时借方在信号量上等待,直到有人归还。</item>
/// </list>
/// </summary>
public class DefaultObjectPool<T> : IObjectPool<T> where T : class
{
    private readonly IObjectPoolPolicy<T> _policy;
    private readonly ConcurrentBag<T> _idle = new();
    // 剩余可用配额,含"已借出 + 空闲"的总数控制。Wait 成功 = 拿到一次租借权。
    private readonly SemaphoreSlim _slots;
    private int _totalCount;

    public DefaultObjectPool(string name, IObjectPoolPolicy<T> policy, int maxSize)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("name cannot be empty", nameof(name));
        if (policy is null) throw new ArgumentNullException(nameof(policy));
        if (maxSize <= 0) throw new ArgumentOutOfRangeException(nameof(maxSize));

        Name = name;
        _policy = policy;
        MaxSize = maxSize;
        _slots = new SemaphoreSlim(maxSize, maxSize);
    }

    public string Name { get; }
    public int MaxSize { get; }
    public int Count => Volatile.Read(ref _totalCount);

    public async ValueTask<IPoolLease<T>> RentAsync(CancellationToken cancellationToken = default)
    {
        // 拿配额(可能因满载阻塞)
        await _slots.WaitAsync(cancellationToken).ConfigureAwait(false);

        T? item = null;
        // 尝试从空闲队列取
        if (!_idle.TryTake(out item))
        {
            // 空闲队列空:申请一个新的槽位
            Interlocked.Increment(ref _totalCount);
            try
            {
                item = await _policy.CreateAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // 创建失败,把配额和计数都退回去
                Interlocked.Decrement(ref _totalCount);
                _slots.Release();
                throw;
            }
        }

        if (item is null)
        {
            // 极端:policy 返回 null,同样回退
            Interlocked.Decrement(ref _totalCount);
            _slots.Release();
            throw new InvalidOperationException(
                $"[EventBus] IObjectPoolPolicy<{typeof(T).Name}>.CreateAsync returned null for pool '{Name}'");
        }

        var lease = new Lease(this, item, Name);
        return lease;
    }

    public void Return(T item, bool isValid)
    {
        if (item is null) return;

        if (isValid && _policy.TryReuse(item))
        {
            // 复用:放回空闲队列
            _idle.Add(item);
        }
        else
        {
            // 不可复用:异步释放对象(框架不强制要求 IAsyncDisposable,这里 Try catch 兜底)
            TryDispose(item);
            Interlocked.Decrement(ref _totalCount);
        }

        // 释放配额,让其他等待者可以借出
        try { _slots.Release(); }
        catch (SemaphoreFullException)
        {
            // 极端 race:Return 被调用了超过 Rent 的次数,记日志不抛。
        }
    }

    private static void TryDispose(object item)
    {
        switch (item)
        {
            case IAsyncDisposable iad:
                // 不 await,Return 必须同步语义。
                _ = iad.DisposeAsync();
                break;
            case IDisposable id:
                id.Dispose();
                break;
        }
    }

    private sealed class Lease : IPoolLease<T>
    {
        private readonly DefaultObjectPool<T> _pool;
        private int _disposed;

        internal Lease(DefaultObjectPool<T> pool, T value, string poolKey)
        {
            _pool = pool;
            Value = value;
            PoolKey = poolKey;
        }

        public T? Value { get; }
        public bool IsValid { get; set; } = true;
        public string PoolKey { get; }

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return ValueTask.CompletedTask;

            if (Value is null)
            {
                // 创建失败时 Dispose 仍需释放配额
                _pool.Return(null!, false);
                return ValueTask.CompletedTask;
            }

            _pool.Return(Value, IsValid);
            return ValueTask.CompletedTask;
        }
    }
}

/// <summary>
/// <see cref="IObjectPoolProvider"/> 的默认实现,内部啥也不存,每次 Create 直接 new。
/// </summary>
public class DefaultObjectPoolProvider : IObjectPoolProvider
{
    public IObjectPool<T> Create<T>(string name, IObjectPoolPolicy<T> policy, int maxSize) where T : class
    {
        return new DefaultObjectPool<T>(name, policy, maxSize);
    }
}
