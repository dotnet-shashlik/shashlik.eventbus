using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using SampleBase;
using Shashlik.EventBus;
using Shashlik.EventBus.MongoDb;
using Shashlik.EventBus.MySql;
using Shashlik.EventBus.Pulsar;
using Shashlik.Utils.Extensions;

namespace Sample.Pulsar.MongoDb
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
                    var mongoClient = new MongoClient(connectionString);
                    services.AddSingleton<IMongoClient>(mongoClient);

                    services.AddLogging(logging => { logging.AddConsole().SetMinimumLevel(LogLevel.Information); });

                    services.AddEventBus(r => { r.Environment = "DemoRedisMySql"; })
                        .AddMongoDb(connectionString)
                        .AddPulsar(configuration.GetSection("EventBus:Pulsar"));

                    services.AddHostedService<TestService>();
                })
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();
        }

        public class TestService : IHostedService
        {
            public TestService(IEventPublisher eventPublisher, IServiceScopeFactory serviceScopeFactory,
                ILogger<TestService> logger, IOptions<EventBusMongoDbOptions> options, IPulsarConnection connection,
                IMongoClient mongoClient)
            {
                EventPublisher = eventPublisher;
                ServiceScopeFactory = serviceScopeFactory;
                Logger = logger;
                Options = options;
                Connection = connection;
                MongoClient = mongoClient;
            }

            private IPulsarConnection Connection { get; }
            private IEventPublisher EventPublisher { get; }
            private IServiceScopeFactory ServiceScopeFactory { get; }
            private ILogger<TestService> Logger { get; }
            private IMongoClient MongoClient { get; }
            private IOptions<EventBusMongoDbOptions> Options { get; }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                while (true)
                {
                    Console.WriteLine("请输入消息内容:");
                    var content = Console.ReadLine();
                    if (content.EqualsIgnoreCase("exit"))
                        return;

                    content = $"{DateTime.Now:HH:mm:ss} [ClusterId: {ClusterId}]=====>{content}";


                    var mongoDatabase = MongoClient.GetDatabase(Options.Value.DataBase);
                    if ((await mongoDatabase.ListCollectionNamesAsync(cancellationToken: cancellationToken)).ToList()
                        .All(r => r != nameof(Users)))
                    {
                        await mongoDatabase.CreateCollectionAsync(nameof(Users), cancellationToken: cancellationToken);
                    }

                    var usersCollection = mongoDatabase.GetCollection<Users>(nameof(Users));
                    using var startSession =
                        await mongoDatabase.Client.StartSessionAsync(cancellationToken: cancellationToken);
                    // 开启事务, mongodb 事务需要集群部署
                    startSession.StartTransaction();

                    try
                    {
                        // 业务数据
                        await usersCollection.InsertOneAsync(startSession, new Users
                        {
                            Id = ObjectId.GenerateNewId().ToString(),
                            Name = Guid.NewGuid().ToString()
                        }, cancellationToken: cancellationToken);

                        // 发布消息
                        await EventPublisher.PublishAsync(new Event1 { Name = content },
                            // 转换为ITransactionContext
                            startSession.GetTransactionContext(),
                            cancellationToken: cancellationToken);

                        if (DateTime.Now.Millisecond % 2 == 0)
                            throw new Exception("模拟异常");

                        // 提交事务
                        await startSession.CommitTransactionAsync(cancellationToken);
                        Console.WriteLine($"已发布消息: {content}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("逻辑异常,数据回滚,发布失败:" + content);
                        // 回滚事务,消息数据也将回滚,不会发布
                        await startSession.AbortTransactionAsync(cancellationToken);
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