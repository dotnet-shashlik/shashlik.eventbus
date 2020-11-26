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
using Shashlik.EventBus.PostgreSQL;
using Shashlik.Utils.Extensions;

namespace Sample.Kafka.PostgreSQL
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

                    services.AddLogging(logging => { logging.AddConsole().SetMinimumLevel(LogLevel.Debug); });

                    services.AddDbContextPool<DemoDbContext>(r =>
                    {
                        r.UseNpgsql(ConnectionString,
                            db => { db.MigrationsAssembly(typeof(DemoDbContext).Assembly.GetName().FullName); });
                    });

                    services.AddEventBus(r => { r.Environment = "DemoKafkaPostgre"; })
                        .AddNpgsql<DemoDbContext>()
                        .AddKafka(r => { r.Properties.Add(new[] {"bootstrap.servers", "192.168.50.178:9092"}); });

                    services.AddHostedService<TestService>();
                })
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();
        }

        public class TestService : IHostedService
        {
            public TestService(IEventPublisher eventPublisher, DemoDbContext dbContext)
            {
                EventPublisher = eventPublisher;
                DbContext = dbContext;
            }

            private IEventPublisher EventPublisher { get; }
            private DemoDbContext DbContext { get; }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                for (var i = 0; i < 10; i++)
                {
                    var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

                    if (i % 3 == 0)
                    {
                        await DbContext.PublishAsync(new Event1 {Name = $"【ClusterId: {ClusterId}】张三: {i}"}, null, cancellationToken);
                        await transaction.RollbackAsync(cancellationToken);
                        await Task.Delay(5, cancellationToken);
                        continue;
                    }

                    if (i % 2 == 0)
                        await DbContext.PublishAsync(new Event1 {Name = $"【ClusterId: {ClusterId}】张三: {i}"}, null, cancellationToken);
                    else
                        await DbContext.PublishAsync(new DelayEvent {Name = $"【ClusterId: {ClusterId}】李四: {i}"},
                            DateTimeOffset.Now.AddSeconds(new Random().Next(6, 100)), null, cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                    await Task.Delay(5, cancellationToken);
                }
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