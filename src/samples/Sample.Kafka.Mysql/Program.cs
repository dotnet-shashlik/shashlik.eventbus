using System;
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
using Shashlik.EventBus.Kafka;
using Shashlik.EventBus.MySql;
using Shashlik.Utils.Extensions;

namespace Sample.Kafka.Mysql
{
    public class Program
    {
        public const string ConnectionString =
            "server=192.168.50.178;database=eventbustest;user=testuser;password=123123;Pooling=True;Min Pool Size=5;Max Pool Size=10;";

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
                        r.UseMySql(ConnectionString,
                            db => { db.MigrationsAssembly(typeof(DemoDbContext).Assembly.GetName().FullName); });
                    }, 5);

                    services.AddEventBus(r => { r.Environment = "DemoKafkaMySql"; })
                        .AddMySql<DemoDbContext>()
                        .AddKafka(r => { r.Properties.Add(new[] {"bootstrap.servers", "192.168.50.178:9092"}); });

                    services.AddHostedService<TestService>();
                })
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();
        }

        public class TestService : IHostedService
        {
            public TestService(IEventPublisher eventPublisher, IServiceScopeFactory serviceScopeFactory)
            {
                EventPublisher = eventPublisher;
                ServiceScopeFactory = serviceScopeFactory;
            }

            private IEventPublisher EventPublisher { get; }
            private IServiceScopeFactory ServiceScopeFactory { get; }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                await Task.CompletedTask;
                Parallel.For(1, 1, new ParallelOptions {MaxDegreeOfParallelism = 10}, async i =>
                {
                    using var scope = ServiceScopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetService<DemoDbContext>();

                    var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

                    if (i % 2 == 0)
                        await EventPublisher.PublishAsync(new Event1 {Name = $"【ClusterId: {ClusterId}】张三: {i}"},
                            new TransactionContext(dbContext, transaction), cancellationToken: cancellationToken);
                    else
                        await EventPublisher.PublishAsync(new DelayEvent {Name = $"【ClusterId: {ClusterId}】李四: {i}"},
                            new TransactionContext(dbContext, transaction), DateTimeOffset.Now.AddSeconds(20),
                            cancellationToken: cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                });
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
            optionsBuilder.UseMySql(Program.ConnectionString);

            return new DemoDbContext(optionsBuilder.Options);
        }
    }
}