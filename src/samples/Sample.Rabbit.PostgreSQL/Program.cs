﻿using System;
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
using Shashlik.EventBus.PostgreSQL;
using Shashlik.EventBus.RabbitMQ;
using Shashlik.Utils.Extensions;

namespace Sample.Rabbit.PostgreSQL
{
    public class Program
    {
        public const string ConnectionString =
            "server=192.168.50.178;user id=testuser;password=123123;persistsecurityinfo=true;database=eventbustest;Pooling=True;Minimum Pool Size=3;Maximum Pool Size=5;";

        public static string ClusterId { get; set; }

        private static async Task Main(string[] args)
        {
            Console.Write($"请输入节点集群ID:");
            ClusterId = Console.ReadLine();
            if (ClusterId.IsNullOrWhiteSpace())
                return;

            var host = new HostBuilder().ConfigureHostConfiguration(configHost => { configHost.AddCommandLine(args); })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<TestEventHandler1>();
                    services.AddTransient<TestEventHandler2>();

                    services.AddLogging(logging => { logging.AddConsole().SetMinimumLevel(LogLevel.Information); });

                    services.AddDbContextPool<DemoDbContext>(r =>
                    {
                        r.UseNpgsql(ConnectionString,
                            db => { db.MigrationsAssembly(typeof(DemoDbContext).Assembly.GetName().FullName); });
                    }, 5);

                    services.AddEventBus(r => { r.Environment = "DemoRabbitMySql"; })
                        .AddNpgsql<DemoDbContext>()
                        .AddRabbitMQ(r =>
                        {
                            r.Host = "192.168.50.178";
                            r.UserName = "rabbit";
                            r.Password = "8NnT2nUNoOwpBAue";
                        });

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
                await Task.CompletedTask;

                for (var i = 0; i < 30000; i++)
                {
                    var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

                    if (i % 3 == 0)
                    {
                        await DbContext.PublishEventAsync(new Event1 {Name = $"【ClusterId: {ClusterId}】张三: {i}"}, null, cancellationToken);
                        await transaction.RollbackAsync(cancellationToken);
                        await Task.Delay(5, cancellationToken);
                        continue;
                    }

                    if (i % 2 == 0)
                        await DbContext.PublishEventAsync(new Event1 {Name = $"【ClusterId: {ClusterId}】张三: {i}"}, null, cancellationToken);
                    else
                        await DbContext.PublishEventAsync(new DelayEvent {Name = $"【ClusterId: {ClusterId}】李四: {i}"},
                            DateTimeOffset.Now.AddSeconds(new Random().Next(6, 100)), null, cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
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

    public class DbContextFactory : IDesignTimeDbContextFactory<DemoDbContext>
    {
        public DemoDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DemoDbContext>();
            optionsBuilder.UseNpgsql(Program.ConnectionString);

            return new DemoDbContext(optionsBuilder.Options);
        }
    }
}