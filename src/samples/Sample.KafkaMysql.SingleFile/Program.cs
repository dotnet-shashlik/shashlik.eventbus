using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FreeSql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shashlik.EventBus;
using Shashlik.EventBus.Kafka;
using Shashlik.EventBus.RelationDbStorage;

namespace Sample.KafkaMysql.SingleFile;

public class Program
{
    public const string DefaultMySqlConn =
        "server=localhost;port=3306;database=eventbustest;user id=root;password=123123;Pooling=True;Minimum Pool Size=5;Maximum Pool Size=10;";

    public const string DefaultKafkaBootstrap = "localhost:9092";

    public const int DefaultAutoCount = 5;

    public static async Task<int> Main(string[] args)
    {
        var mode = ParseMode(args);
        var mySqlConn = Environment.GetEnvironmentVariable("MYSQL_CONN") ?? DefaultMySqlConn;
        var kafkaBootstrap = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP") ?? DefaultKafkaBootstrap;
        var autoCount = int.TryParse(Environment.GetEnvironmentVariable("AUTO_COUNT"), out var n) ? n : DefaultAutoCount;

        Console.WriteLine("=== Sample.KafkaMysql.SingleFile ===");
        Console.WriteLine($"Mode              : {mode}");
        Console.WriteLine($"MySQL             : {MaskPassword(mySqlConn)}");
        Console.WriteLine($"Kafka bootstrap   : {kafkaBootstrap}");
        Console.WriteLine($"Auto publish count: {autoCount}");
        Console.WriteLine();

        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.ClearProviders()
            .AddSimpleConsole(o =>
            {
                o.SingleLine = true;
                o.TimestampFormat = "HH:mm:ss ";
            })
            .SetMinimumLevel(LogLevel.Debug);

        builder.Services.AddEventBus(opts =>
            {
                opts.Environment = "Sample-KafkaMysql-SingleFile";

                // 单文件部署兜底: 显式声明 handler 所在程序集,
                // 跳过 DependencyContext 反射链,确保 handler 一定能被发现。
                // opts.HandlerAssemblies.Add(typeof(Program).Assembly);
            })
            .AddRelationDb(o => o.UseConnection(DataType.MySql, mySqlConn))
            .AddKafka(new Dictionary<string, string>
            {
                { "bootstrap.servers", kafkaBootstrap },
                { "allow.auto.create.topics", "true" }
            });

        builder.Services.AddHostedService(sp => new TestRunner(
            sp.GetRequiredService<IEventPublisher>(),
            sp.GetRequiredService<ILogger<TestRunner>>(),
            sp.GetRequiredService<IHostApplicationLifetime>(),
            mode,
            autoCount));

        using var host = builder.Build();
        await host.RunAsync();

        var ok = TestRunner.PublishedCount == autoCount
                 && TestRunner.ConsumedCount >= autoCount;
        Console.WriteLine();
        Console.WriteLine($"=== Result: published={TestRunner.PublishedCount}, consumed={TestRunner.ConsumedCount} ===");
        return ok ? 0 : 1;
    }

    private static string ParseMode(string[] args)
    {
        return args.Any(a =>
                   a.Equals("--interactive", StringComparison.OrdinalIgnoreCase) ||
                   a.Equals("-i", StringComparison.OrdinalIgnoreCase))
            ? "interactive"
            : "auto";
    }

    private static string MaskPassword(string conn)
    {
        var idx = conn.IndexOf("password=", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return conn;
        var start = idx + "password=".Length;
        var end = conn.IndexOf(';', start);
        if (end < 0) end = conn.Length;
        return conn.Substring(0, start) + "***" + conn.Substring(end);
    }
}

public class TestRunner : IHostedService
{
    private static int _publishedCount;
    private static int _consumedCount;
    private static readonly HashSet<int> _consumedIds = new();
    private static readonly object _consumedIdsLock = new();

    public static int PublishedCount => _publishedCount;

    public static int ConsumedCount => _consumedCount;

    public static void RecordConsumed(int id)
    {
        Interlocked.Increment(ref _consumedCount);
        lock (_consumedIdsLock)
        {
            _consumedIds.Add(id);
        }
    }

    private readonly IEventPublisher _publisher;
    private readonly ILogger<TestRunner> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly string _mode;
    private readonly int _autoCount;

    public TestRunner(
        IEventPublisher publisher,
        ILogger<TestRunner> logger,
        IHostApplicationLifetime lifetime,
        string mode,
        int autoCount)
    {
        _publisher = publisher;
        _logger = logger;
        _lifetime = lifetime;
        _mode = mode;
        _autoCount = autoCount;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        // 等 EventBusStartup 完成: storage 初始化 + handler 订阅 + 接收器就绪
        await Task.Delay(2000, ct);

        if (_mode == "auto")
            await RunAuto(ct);
        else
            await RunInteractive(ct);
    }

    private async Task RunAuto(CancellationToken ct)
    {
        _logger.LogInformation("[AUTO] publishing {Count} events ...", _autoCount);

        for (var i = 0; i < _autoCount; i++)
        {
            var evt = new TestEvent { Id = i, Message = $"auto-{i}-{DateTime.Now:HHmmssfff}" };
            await _publisher.PublishAsync(evt, transactionContext: null, cancellationToken: ct);
            Interlocked.Increment(ref _publishedCount);
            _logger.LogInformation("[PUBLISH] Id={Id}, Message={Message}", evt.Id, evt.Message);
            await Task.Delay(300, ct);
        }

        _logger.LogInformation("[AUTO] all events published, waiting up to 10s for consumption ...");

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline && _consumedCount < _autoCount && !ct.IsCancellationRequested)
            await Task.Delay(500, ct);

        lock (_consumedIdsLock)
        {
            var missing = Enumerable.Range(0, _autoCount).Except(_consumedIds).ToList();
            if (missing.Count > 0)
                _logger.LogWarning("[AUTO] missing consumed ids: {Missing}", string.Join(",", missing));
        }

        _logger.LogInformation("[AUTO] stopping host ...");
        _lifetime.StopApplication();
    }

    private async Task RunInteractive(CancellationToken ct)
    {
        _logger.LogInformation("[INTERACTIVE] type a message and press Enter to publish (or 'exit' to quit)");

        while (!ct.IsCancellationRequested)
        {
            Console.Write("> ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
                continue;
            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            var evt = new TestEvent { Id = Environment.TickCount, Message = input };
            await _publisher.PublishAsync(evt, transactionContext: null, cancellationToken: ct);
            Interlocked.Increment(ref _publishedCount);
            _logger.LogInformation("[PUBLISH] Id={Id}, Message={Message}", evt.Id, evt.Message);
        }

        _lifetime.StopApplication();
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
