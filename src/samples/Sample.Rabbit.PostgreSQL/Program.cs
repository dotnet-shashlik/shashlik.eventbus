using System;
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
using Shashlik.EventBus.PostgreSQL;
using Shashlik.EventBus.RabbitMQ;
using Shashlik.Utils.Extensions;

namespace Sample.Rabbit.PostgreSQL
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
                        r.UseNpgsql(connectionString,
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
                while (true)
                {
                    Console.WriteLine("请输入消息内容:");
                    var content = Console.ReadLine();
                    if (content.EqualsIgnoreCase("exit"))
                        return;

                    content = $"{DateTime.Now:HH:mm:ss} [ClusterId: {ClusterId}]=====>{content}";

                    // 本地事务
                    if (DateTime.Now.Millisecond % 2 == 0)
                    {
                        await using var tran = await DbContext.Database.BeginTransactionAsync(cancellationToken);
                        try
                        {
                            // 业务数据
                            DbContext.Users.Add(new Users
                            {
                                Name = Guid.NewGuid().ToString()
                            });

                            // 发布事件
                            await EventPublisher.PublishAsync(new Event1 { Name = content },
                                DbContext.GetTransactionContext(),
                                cancellationToken: cancellationToken);

                            if (DateTime.Now.Millisecond % 2 == 0)
                                throw new Exception("模拟异常");

                            // 提交事务
                            await tran.CommitAsync(cancellationToken);
                            Console.WriteLine($"已发布消息: {content}");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("逻辑异常,数据回滚,发布失败:" + content);
                            // 回滚事务,消息数据也将回滚,不会发布
                            await tran.RollbackAsync(cancellationToken);
                        }
                    }
                    // TransactionScope
                    else
                    {
                        using var tran = new System.Transactions.TransactionScope();
                        try
                        {
                            // 业务数据
                            DbContext.Users.Add(new Users
                            {
                                Name = Guid.NewGuid().ToString()
                            });

                            // 发布事件
                            await EventPublisher.PublishAsync(new Event1 { Name = content },
                                XaTransactionContext.Current,
                                cancellationToken: cancellationToken);

                            if (DateTime.Now.Millisecond % 2 == 0)
                                throw new Exception("模拟异常");

                            // 提交事务
                            tran.Complete();
                            Console.WriteLine($"已发布消息: {content}");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("逻辑异常,数据回滚,发布失败:" + content);
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