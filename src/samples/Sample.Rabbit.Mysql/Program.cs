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
using Shashlik.EventBus.MySql;
using Shashlik.EventBus.RabbitMQ;
using Shashlik.Utils.Extensions;

namespace Sample.Rabbit.Mysql
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

                    services.AddLogging(logging => { logging.AddConsole().SetMinimumLevel(LogLevel.Warning); });

                    services.AddDbContextPool<DemoDbContext>(r =>
                    {
                        r.UseMySql(ConnectionString,
                            db => { db.MigrationsAssembly(typeof(DemoDbContext).Assembly.GetName().FullName); });
                    }, 5);

                    services.AddEventBus(r => { r.Environment = "DemoRabbitMySql"; })
                        .AddMySql<DemoDbContext>()
                        .AddEfCoreExtensions<DemoDbContext>()
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
                        await EventPublisher.PublishAsync(new Event1 {Name = $"【ClusterId: {ClusterId}】王五: {i}"},
                            cancellationToken: cancellationToken);
                        await transaction.RollbackAsync(cancellationToken);
                        continue;
                    }

                    if (i % 2 == 0)
                        await EventPublisher.PublishAsync(new Event1 {Name = $"【ClusterId: {ClusterId}】张三: {i}"},
                            cancellationToken: cancellationToken);
                    else
                        await EventPublisher.PublishAsync(new DelayEvent {Name = $"【ClusterId: {ClusterId}】李四: {i}"},
                            DateTimeOffset.Now.AddSeconds(new Random().Next(6, 100)),
                            cancellationToken: cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                    Thread.Sleep(5);
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
            optionsBuilder.UseMySql(Program.ConnectionString);

            return new DemoDbContext(optionsBuilder.Options);
        }
    }
}