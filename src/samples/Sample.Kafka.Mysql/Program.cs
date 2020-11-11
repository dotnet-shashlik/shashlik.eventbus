using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySqlSampleBase;
using Shashlik.EventBus;
using Shashlik.EventBus.Kafka;
using Shashlik.EventBus.MySql;

namespace Sample.Kafka.Mysql
{
    public class Program
    {
        public const string ConnectionString =
            "server=192.168.50.178;database=eventbustest;user=root;password=jizhen.cool.0416;Pooling=True;Min Pool Size=3;Max Pool Size=5;";

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
                        r.UseMySql(ConnectionString,
                            db => { db.MigrationsAssembly(typeof(DemoDbContext).Assembly.GetName().FullName); });
                    });

                    services.AddEventBus(r => { r.Environment = "DemoKafka"; })
                        .AddMySql<DemoDbContext>()
                        .AddKafka(r => { r.Base.BootstrapServers = "192.168.50.178:9092"; });

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
                for (int i = 0; i < 0; i++)
                {
                    var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

                    // await EventPublisher.PublishAsync(new Event1 {Name = $"张三: {i}"},
                    //     new TransactionContext(DbContext, transaction), cancellationToken: cancellationToken);
                    await EventPublisher.PublishAsync(new DelayEvent {Name = $"李四: {i}"},
                        new TransactionContext(DbContext, transaction), DateTimeOffset.Now.AddSeconds(10),
                        cancellationToken: cancellationToken);

                    if (i == 2 || i == 4)
                    {
                        Console.WriteLine("rollback");
                        await transaction.RollbackAsync(cancellationToken);
                        continue;
                    }

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
            optionsBuilder.UseMySql(Program.ConnectionString);

            return new DemoDbContext(optionsBuilder.Options);
        }
    }
}