﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SampleBase;
using Shashlik.EventBus;
using Shashlik.EventBus.RabbitMQ;
using Shashlik.EventBus.SqlServer;
using Shashlik.Utils.Extensions;

namespace Sample.Rabbit.SqlServer
{
    // dotnet ef migrations add init -c DemoDbContext  -o SqlServerMigrations -p ./samples/SampleBase/SampleBase.csproj -s ./samples/Sample.Rabbit.SqlServer/Sample.Rabbit.SqlServer.csproj
    public class Program
    {
        public static string ClusterId { get; set; }

        private static async Task Main(string[] args)
        {
            // 可以启动多个实例, 不同实例输入不同的集群节点id,逐个发布消息可以查看负载情况
            Console.Write($"请输入节点集群ID:");
            ClusterId = Console.ReadLine();
            if (ClusterId.IsNullOrWhiteSpace())
                return;

            var host = new HostBuilder()
                .ConfigureHostConfiguration(config =>
                {
                    config.AddCommandLine(args);
                    var file = new FileInfo("./config.yaml").FullName;
                    config.AddYamlFile(file);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    using var serviceProvider = services.BuildServiceProvider();
                    var configuration = serviceProvider.GetService<IConfiguration>();
                    var connectionString = configuration.GetConnectionString("Default");
                    services.AddLogging(logging => { logging.AddConsole().SetMinimumLevel(LogLevel.Information); });

                    services.AddDbContextPool<DemoDbContext>(r =>
                    {
                        r.UseSqlServer(connectionString,
                            db => { db.MigrationsAssembly(typeof(DemoDbContext).Assembly.GetName().FullName); });
                    }, 5);

                    services.AddEventBus(r => { r.Environment = "DemoRabbitSqlServer"; })
                        .AddSqlServer<DemoDbContext>()
                        .AddRabbitMQ(configuration.GetSection("EventBus:RabbitMQ"));

                    services.AddHostedService<TestService>();
                })
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();
        }

        public class TestService : IHostedService
        {
            public TestService(IEventPublisher eventPublisher, IServiceScopeFactory serviceScopeFactory,
                ILogger<TestService> logger, DemoDbContext dbContext)
            {
                EventPublisher = eventPublisher;
                ServiceScopeFactory = serviceScopeFactory;
                Logger = logger;
                DbContext = dbContext;
            }

            private IEventPublisher EventPublisher { get; }
            private IServiceScopeFactory ServiceScopeFactory { get; }
            private ILogger<TestService> Logger { get; }
            private DemoDbContext DbContext { get; }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                while (true)
                {
                    Console.WriteLine("请输入消息内容:");
                    var content = Console.ReadLine();
                    if (content.EqualsIgnoreCase("exit"))
                        return;

                    content = $"{DateTime.Now:HH:mm:ss} [ClusterId: {ClusterId}]=====>{content}";

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