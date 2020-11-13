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

namespace Sample.Kafka.PostgreSQL
{
    public class Program
    {
        public const string ConnectionString =
            "server=192.168.50.178;user id=testuser;password=123123;persistsecurityinfo=true;database=eventbustest;Pooling=True;Minimum Pool Size=3;Maximum Pool Size=5;";

        private static async Task Main(string[] args)
        {
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
                        Console.WriteLine("rollback");
                        await transaction.RollbackAsync(cancellationToken);
                        continue;
                    }

                    if (i % 2 == 0)
                        await EventPublisher.PublishAsync(new Event1 {Name = $"张三: {i}"},
                            new TransactionContext(DbContext, transaction), cancellationToken: cancellationToken);
                    else
                        await EventPublisher.PublishAsync(new DelayEvent {Name = $"李四: {i}"},
                            new TransactionContext(DbContext, transaction), DateTimeOffset.Now.AddSeconds(20),
                            cancellationToken: cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                    await Task.Delay(1000, cancellationToken);
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