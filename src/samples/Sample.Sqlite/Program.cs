// dotnet ef migrations add sqlite_init -c DemoDbContext -o Migrations -p ./Sample.Sqlite/Sample.Sqlite.csproj -s ./Sample.Sqlite/Sample.Sqlite.csproj

using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SampleBase;
using Shashlik.EventBus;
using Shashlik.EventBus.MemoryQueue;
using Shashlik.EventBus.Sqlite;
using Shashlik.Utils.Extensions;

namespace Sample.Sqlite
{
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
                        r.EnableSensitiveDataLogging();
                        r.UseSqlite(connectionString,
                            db => { db.MigrationsAssembly("Sample.Sqlite"); });
                    }, 5);

                    services.AddEventBus(r => { r.Environment = "DemoRabbitSqlite"; })
                        .AddSqlite<DemoDbContext>()
                        .AddMemoryQueue();

                    services.AddHostedService<TestService>();

                    using var serviceProvider2 = services.BuildServiceProvider();
                    using var serviceScope = serviceProvider2.CreateScope();
                    serviceScope.ServiceProvider.GetRequiredService<DemoDbContext>()
                        .Database.Migrate();
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
                while (true)
                {
                    Console.WriteLine("请输入消息内容:");
                    var content = Console.ReadLine();
                    if (content.EqualsIgnoreCase("exit"))
                        return;

                    content = $"{DateTime.Now:HH:mm:ss} [ClusterId: {ClusterId}]=====>{content}";
                    using var scope = ServiceScopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<DemoDbContext>();

                    // 本地事务
                    {
                        await using var tran = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                        try
                        {
                            // 业务数据
                            dbContext.Users.Add(new Users
                            {
                                Name = Guid.NewGuid().ToString()
                            });

                            await dbContext.SaveChangesAsync(cancellationToken);

                            // 发布事件
                            await EventPublisher.PublishAsync(new Event1 { Name = content },
                                dbContext.GetTransactionContext(),
                                cancellationToken: cancellationToken);

                            if (DateTime.Now.Millisecond % 2 == 0)
                                throw new Exception("模拟异常");

                            // 提交事务
                            await tran.CommitAsync(cancellationToken);
                            Console.WriteLine($"已发布消息: {content}");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.StackTrace);
                            Console.WriteLine("逻辑异常,数据回滚,发布失败:" + content);
                            // 回滚事务,消息数据也将回滚,不会发布
                            await tran.RollbackAsync(cancellationToken);
                        }
                    }
                }
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}