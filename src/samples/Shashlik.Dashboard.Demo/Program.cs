using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Shashlik.Dashboard.Demo;
using Shashlik.EventBus;
using Shashlik.EventBus.Dashboard;
using Shashlik.EventBus.MemoryQueue;
using Shashlik.EventBus.MongoDb;
using Shashlik.EventBus.MySql;
using Shashlik.EventBus.PostgreSQL;
using Shashlik.EventBus.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

Console.WriteLine("请选择数据库类型:");
Console.WriteLine("1: mysql");
Console.WriteLine("2: postgres");
Console.WriteLine("3: sqlserver");
Console.WriteLine("4: mongodb");

// var type = Console.ReadLine();
var type = "1";

string connectionString;
switch (type)
{
    case "1":
        connectionString = builder.Configuration.GetValue<string>("mysql");
        break;
    case "2":
        connectionString = builder.Configuration.GetValue<string>("postgres");
        break;
    case "3":
        connectionString = builder.Configuration.GetValue<string>("sqlserver");
        break;
    case "4":
        connectionString = builder.Configuration.GetValue<string>("mongodb");
        builder.Services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
        break;
    default:
        throw new ArgumentException();
}

if (type != "4")
{
    builder.Services.AddDbContext<DataContext>(
        x =>
        {
            switch (type)
            {
                case "1":
                    x.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    break;
                case "2":
                    x.UseNpgsql(connectionString);
                    break;
                case "3":
                    x.UseSqlServer(connectionString);
                    break;
                default:
                    break;
            }
        });
}

var eventBusBuilder = builder.Services.AddEventBus(r =>
{
    // 这些都是缺省配置，可以直接services.AddEventBus()
    // 运行环境，注册到MQ的事件名称和事件处理名称会带上此后缀
    r.Environment = "Production";
    // 最大失败重试次数，默认60次
    r.RetryFailedMax = 60;
    // 消息重试间隔，默认2分钟
    r.RetryInterval = 5;
    // 单次重试消息数量限制，默认100
    r.RetryLimitCount = 100;
    // 成功的消息过期时间，默认3天，失败的消息永不过期，必须处理
    r.SucceedExpireHour = 24 * 3;
    // 消息处理失败后，重试器介入时间，默认5分钟后
    r.StartRetryAfter = 20;
    // 事务提交超时时间,单位秒,默认60秒
    r.TransactionCommitTimeout = 10;
    // 重试器执行时消息锁定时长
    r.LockTime = 2;
});
switch (type)
{
    case "1":
        eventBusBuilder = eventBusBuilder.AddMySql<DataContext>();
        break;
    case "2":
        eventBusBuilder = eventBusBuilder.AddNpgsql<DataContext>();
        break;
    case "3":
        eventBusBuilder = eventBusBuilder.AddSqlServer<DataContext>();
        break;
    case "4":
        eventBusBuilder = eventBusBuilder.AddMongoDb(connectionString);
        break;
    default:
        throw new ArgumentException();
}

// 使用ef DbContext mysql
eventBusBuilder
    .AddMemoryQueue()
    // 注册dashboard service, 并使用自定义认证类TokenCookieAuth
    .AddDashboard(options =>
    {
        // 指定认证类
        // options.UseAuthenticate<SecretCookieAuthenticate>();
        // 使用SecretAuthenticate认证
        options.UseSecretAuthenticate("Shashlik.EventBus.Secret");
    })
    ;

var app = builder.Build();

if (type != "4")
{
    using var serviceScope = app.Services.CreateScope();
    var dataContext = serviceScope.ServiceProvider.GetRequiredService<DataContext>();
    dataContext.Database.Migrate();
}

// Configure the HTTP request pipeline.

app.UseAuthorization();
app.UseRouting();
// 启用 dashboard
app.UseEventBusDashboard();

app.MapControllers();

app.Run();