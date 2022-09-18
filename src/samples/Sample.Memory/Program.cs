using System;
using System.Collections.Generic;
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
                    services.AddLogging(logging => { logging.AddConsole().SetMinimumLevel(LogLevel.Information); });

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

                while (true)
                {
                    Console.WriteLine("请输入消息内容:");
                    var content = Console.ReadLine();
                    if (content.EqualsIgnoreCase("exit"))
                        return;

                    content = $"{DateTime.Now:HH:mm:ss}=====>{content}";

                    Console.WriteLine($"已发布消息: {content}");

                    await EventPublisher.PublishAsync(new Event1 { Name = content },
                        null,
                        cancellationToken: cancellationToken);
                }
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}