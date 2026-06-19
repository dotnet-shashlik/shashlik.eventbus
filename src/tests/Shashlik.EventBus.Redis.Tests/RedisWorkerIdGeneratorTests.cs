using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FreeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetEscapades.Configuration.Yaml;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using ITimer = Shashlik.EventBus.Utils.ITimer;

namespace Shashlik.EventBus.Redis.Tests;

public class RedisWorkerIdGeneratorTests : IAsyncDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly RedisClient _redisClient;
    private readonly string _appName;
    private readonly ServiceProvider _serviceProvider;

    public RedisWorkerIdGeneratorTests(ITestOutputHelper output)
    {
        _output = output;
        _appName = $"TEST_{Guid.NewGuid():N}";

        var envName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "";
        var configFile = envName.Equals("GitHub", StringComparison.OrdinalIgnoreCase)
            ? "config.test-github.yaml"
            : "config.test.yaml";

        var configuration = new ConfigurationBuilder()
            .AddYamlFile(Path.Combine(Directory.GetCurrentDirectory(), configFile))
            .Build();

        var redisConn = configuration.GetValue<string>("EventBus:Redis:Conn");
        _redisClient = new RedisClient(redisConn);

        var services = new ServiceCollection();
        services.AddSingleton(_redisClient);
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Debug));
        services.AddSingleton<ITimer, DefaultTimer>();
        _serviceProvider = services.BuildServiceProvider();
    }

    private RedisWorkerIdGenerator CreateGenerator(string? appName = null)
    {
        var name = appName ?? _appName;
        var options = Options.Create(new EventBusRedisWorkerIdOptions
        {
            AppName = name,
            RedisClientFactory = sp => sp.GetService<RedisClient>()
        });
        var logger = _serviceProvider.GetRequiredService<ILogger<RedisWorkerIdGenerator>>();
        var timer = _serviceProvider.GetRequiredService<ITimer>();

        return new RedisWorkerIdGenerator(logger, options, _serviceProvider, timer);
    }

    [Fact]
    public async Task GetWorkerId_Should_Return_Value_Between_0_And_1024()
    {
        await using var generator = CreateGenerator();

        var workerId = generator.GetWorkerId();

        workerId.ShouldBeInRange((ushort)0, (ushort)1023);
    }

    [Fact]
    public async Task GetWorkerId_Should_Set_Redis_Key_With_TTL()
    {
        await using var generator = CreateGenerator();
        var workerId = generator.GetWorkerId();

        var key = $"SHASHLIK:EVENTBUS_WORKERID:{_appName}:{workerId}";
        var ttl = _redisClient.Ttl(key);

        ttl.ShouldBeGreaterThan(0);
        ttl.ShouldBeLessThanOrEqualTo(60);
    }

    [Fact]
    public async Task Multiple_Generators_Should_Get_Different_WorkerIds()
    {
        var appName = $"MULTI_{Guid.NewGuid():N}";
        await using var gen1 = CreateGenerator(appName);
        await using var gen2 = CreateGenerator(appName);
        await using var gen3 = CreateGenerator(appName);

        var id1 = gen1.GetWorkerId();
        var id2 = gen2.GetWorkerId();
        var id3 = gen3.GetWorkerId();

        id1.ShouldNotBe(id2);
        id1.ShouldNotBe(id3);
        id2.ShouldNotBe(id3);
    }

    [Fact]
    public async Task DisposeAsync_Should_Release_WorkerId()
    {
        var appName = $"DISPOSE_{Guid.NewGuid():N}";
        var generator = CreateGenerator(appName);
        var workerId = generator.GetWorkerId();

        var key = $"SHASHLIK:EVENTBUS_WORKERID:{appName}:{workerId}";
        _redisClient.Exists(key).ShouldBeTrue();

        await generator.DisposeAsync();

        _redisClient.Exists(key).ShouldBeFalse();
    }

    [Fact]
    public async Task After_Dispose_Released_WorkerId_Can_Be_Reacquired()
    {
        var appName = $"REACQUIRE_{Guid.NewGuid():N}";
        var generator = CreateGenerator(appName);
        var workerId = generator.GetWorkerId();

        await generator.DisposeAsync();

        await using var newGenerator = CreateGenerator(appName);
        var newWorkerId = newGenerator.GetWorkerId();

        newWorkerId.ShouldBe(workerId);
    }

    [Fact]
    public async Task NextId_Should_Generate_Unique_Ids()
    {
        await using var generator = CreateGenerator();

        var id1 = generator.NextId();
        var id2 = generator.NextId();
        var id3 = generator.NextId();

        id1.ShouldNotBe(id2);
        id2.ShouldNotBe(id3);
        id1.ShouldNotBe(0);
    }

    [Fact]
    public async Task GetWorkerId_Should_Return_Same_Value_On_Multiple_Calls()
    {
        await using var generator = CreateGenerator();

        var id1 = generator.GetWorkerId();
        var id2 = generator.GetWorkerId();
        var id3 = generator.GetWorkerId();

        id1.ShouldBe(id2);
        id2.ShouldBe(id3);
    }

    [Fact]
    public async Task RenewLease_Should_Extend_TTL()
    {
        var appName = $"RENEW_{Guid.NewGuid():N}";
        var generator = CreateGenerator(appName);
        var workerId = generator.GetWorkerId();
        var key = $"SHASHLIK:EVENTBUS_WORKERID:{appName}:{workerId}";

        _redisClient.Expire(key, 5);

        await Task.Delay(500);

        var ttlBefore = _redisClient.Ttl(key);
        ttlBefore.ShouldBeLessThanOrEqualTo(5);

        _redisClient.Expire(key, 60);

        var ttlAfter = _redisClient.Ttl(key);
        ttlAfter.ShouldBeGreaterThan(5);

        await generator.DisposeAsync();
    }

    [Fact]
    public async Task Different_AppNames_Should_Get_Same_WorkerId()
    {
        var app1 = $"APP1_{Guid.NewGuid():N}";
        var app2 = $"APP2_{Guid.NewGuid():N}";

        await using var gen1 = CreateGenerator(app1);
        await using var gen2 = CreateGenerator(app2);

        var id1 = gen1.GetWorkerId();
        var id2 = gen2.GetWorkerId();

        id1.ShouldBe(id2);
    }

    [Fact]
    public async Task DisposeAsync_Should_Not_Delete_Other_Instance_Key()
    {
        var appName = $"COMPETE_{Guid.NewGuid():N}";
        var gen1 = CreateGenerator(appName);
        var gen2 = CreateGenerator(appName);

        var id1 = gen1.GetWorkerId();
        var id2 = gen2.GetWorkerId();
        id1.ShouldNotBe(id2);

        var key1 = $"SHASHLIK:EVENTBUS_WORKERID:{appName}:{id1}";
        var key2 = $"SHASHLIK:EVENTBUS_WORKERID:{appName}:{id2}";

        await gen1.DisposeAsync();

        _redisClient.Exists(key1).ShouldBeFalse();
        _redisClient.Exists(key2).ShouldBeTrue();

        await gen2.DisposeAsync();
        _redisClient.Exists(key2).ShouldBeFalse();
    }

    [Fact]
    public async Task Multiple_Generators_Sequential_Reuse()
    {
        var appName = $"SEQ_{Guid.NewGuid():N}";

        {
            var gen1 = CreateGenerator(appName);
            var id1 = gen1.GetWorkerId();
            id1.ShouldBe((ushort)0);
            await gen1.DisposeAsync();
        }

        {
            var gen2 = CreateGenerator(appName);
            var id2 = gen2.GetWorkerId();
            id2.ShouldBe((ushort)0);
            await gen2.DisposeAsync();
        }
    }

    [Fact]
    public async Task Redis_Key_Contains_InstanceId_As_Value()
    {
        await using var generator = CreateGenerator();
        var workerId = generator.GetWorkerId();

        var key = $"SHASHLIK:EVENTBUS_WORKERID:{_appName}:{workerId}";
        var value = _redisClient.Get(key);

        value.ShouldNotBeNull();
        value.ShouldNotBeEmpty();
    }

    public async ValueTask DisposeAsync()
    {
        var keys = _redisClient.Keys($"SHASHLIK:EVENTBUS_WORKERID:TEST_*:*");
        if (keys != null)
        {
            foreach (var key in keys)
            {
                _redisClient.Del(key);
            }
        }

        _serviceProvider.Dispose();
        _redisClient.Dispose();

        await ValueTask.CompletedTask;
    }

    private class DefaultTimer : ITimer
    {
        public Task SetTimeoutAsync(Func<Task> action, TimeSpan expire,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SetTimeoutAsync(Func<Task> action, DateTimeOffset runAt,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public CancellationTokenSource SetInterval(Action action, TimeSpan interval,
            CancellationToken cancellationToken = default)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            return cts;
        }

        public CancellationTokenSource SetInterval(Func<Task> action, TimeSpan interval,
            CancellationToken cancellationToken = default)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            return cts;
        }
    }
}