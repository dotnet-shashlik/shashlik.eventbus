using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.DefaultImpl;
using Yitter.IdGenerator;
using ITimer = Shashlik.EventBus.Utils.ITimer;

namespace Shashlik.EventBus.Redis;

public sealed class RedisWorkerIdGenerator : IIdGenerator, IAsyncDisposable
{
    private readonly string _instanceId = Guid.NewGuid().ToString("N");
    private readonly string _workerKeyPrefix;
    private readonly ushort _maxWorkerId;
    private readonly int _expireSeconds;
    private readonly ILogger<RedisWorkerIdGenerator> _logger;
    private readonly IOptions<EventBusRedisWorkerIdOptions> _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly Lazy<ushort> _workerId;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public RedisWorkerIdGenerator(ILogger<RedisWorkerIdGenerator> logger,
        IOptions<EventBusRedisWorkerIdOptions> options,
        IServiceProvider serviceProvider, ITimer timer)
    {
        _logger = logger;
        _options = options;
        _serviceProvider = serviceProvider;
        _maxWorkerId = 1024;
        _expireSeconds = 60;

        _workerId = new Lazy<ushort>(AcquireWorkerId);
        _workerKeyPrefix = $"SHASHLIK:EVENTBUS_WORKERID:{options.Value.AppName}";

        _cancellationTokenSource = timer.SetInterval(RenewLease, TimeSpan.FromMilliseconds(_expireSeconds * 500));

        YitIdHelper.SetIdGenerator(new IdGeneratorOptions
        {
            WorkerId = GetWorkerId(),
            WorkerIdBitLength = 10
        });
    }

    public ushort GetWorkerId()
    {
        return _workerId.Value;
    }

    /// <summary>
    /// 获取WorkerId
    /// </summary>
    private ushort AcquireWorkerId()
    {
        var redis = _options.Value.RedisClientFactory?.Invoke(_serviceProvider) ??
                    throw new InvalidOperationException("EventBusRedisWorkerIdOptions.RedisClientFactory error");
        for (ushort workerId = 0; workerId <= _maxWorkerId; workerId++)
        {
            var key = $"{_workerKeyPrefix}:{workerId}";

            var success = redis.SetNx(
                key,
                _instanceId,
                _expireSeconds);

            if (success)
                return workerId;
        }

        throw new InvalidOperationException("No available WorkerId");
    }

    /// <summary>
    /// 续约
    /// </summary>
    private void RenewLease()
    {
        var redis = _options.Value.RedisClientFactory?.Invoke(_serviceProvider) ??
                    throw new InvalidOperationException("EventBusRedisWorkerIdOptions.RedisClientFactory error");
        try
        {
            var key = $"{_workerKeyPrefix}:{_workerId.Value}";

            var value = redis.Get(key);

            if (value == _instanceId)
            {
                redis.Expire(key, _expireSeconds);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while renewing Redis lease");
        }
    }

    public long NextId()
    {
        return YitIdHelper.NextId();
    }

    /// <summary>
    /// 主动释放
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await _cancellationTokenSource.CancelAsync();
            _cancellationTokenSource.Dispose();
        }
        catch
        {
            //ignore
        }

        var redis = _options.Value.RedisClientFactory?.Invoke(_serviceProvider) ??
                    throw new InvalidOperationException("EventBusRedisWorkerIdOptions.RedisClientFactory error");
        try
        {
            var key = $"{_workerKeyPrefix}:{_workerId.Value}";

            var value = await redis.GetAsync(key);

            if (value == _instanceId)
            {
                await redis.DelAsync(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while disposing RedisWorkerIdGenerator");
        }
    }
}