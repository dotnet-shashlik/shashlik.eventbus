using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SampleBase;
using Shashlik.EventBus;
using Shashlik.EventBus.MemoryQueue;
using Shashlik.EventBus.MemoryStorage;
using Shashlik.Utils.Extensions;

namespace Sample.Memory
{
    public class Program
    {
        public const string ConnectionString =
            "server=192.168.50.178;database=eventbustest;user=testuser;password=123123;Pooling=True;Min Pool Size=5;Max Pool Size=10;";

        public static string ClusterId { get; set; } = "memory";

        private static async Task Main(string[] args)
        {
            var host = new HostBuilder().ConfigureHostConfiguration(configHost => { configHost.AddCommandLine(args); })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<TestEventHandler1>();
                    services.AddTransient<TestEventHandler2>();

                    services.AddLogging(logging => { logging.AddConsole().SetMinimumLevel(LogLevel.Debug); });

                    services.AddEventBus(r => { r.Environment = "DemoMemory"; })
                        .AddMemoryQueue()
                        .AddMemoryStorage();

                    services.AddHostedService<TestService>();
                })
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();
        }

        public class TestService : IHostedService
        {
            public TestService(IEventPublisher eventPublisher, IServiceScopeFactory serviceScopeFactory,
                ILogger<TestService> logger)
            {
                EventPublisher = eventPublisher;
                ServiceScopeFactory = serviceScopeFactory;
                Logger = logger;
            }

            private IEventPublisher EventPublisher { get; }
            private IServiceScopeFactory ServiceScopeFactory { get; }
            private ILogger<TestService> Logger { get; }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                await Task.CompletedTask;

                for (var i = 0; i < 10; i++)
                {
                    if (i % 2 == 0)
                        await EventPublisher.PublishAsync(new Event1 {Name = $"【ClusterId: {ClusterId}】张三: {i}"}, null,
                            cancellationToken: cancellationToken);
                    else
                        await EventPublisher.PublishAsync(new DelayEvent {Name = $"【ClusterId: {ClusterId}】李四: {i}"}, null,
                            DateTimeOffset.Now.AddSeconds(new Random().Next(6, 100)),
                            cancellationToken: cancellationToken);

                    await Task.Delay(5, cancellationToken);
                }

                Logger.LogWarning($"all message send completed.");
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}