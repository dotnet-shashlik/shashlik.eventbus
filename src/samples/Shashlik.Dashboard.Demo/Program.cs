using Microsoft.EntityFrameworkCore;
using Shashlik.Dashboard.Demo;
using Shashlik.EventBus;
using Shashlik.EventBus.Dashboard;
using Shashlik.EventBus.MemoryQueue;
using Shashlik.EventBus.MySql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
var connectionString = builder.Configuration.GetValue<string>("Mysql");
builder.Services.AddDbContext<DataContext>(
    x => x.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
builder.Services.AddEventBus(r =>
    {
        // 这些都是缺省配置，可以直接services.AddEventBus()
        // 运行环境，注册到MQ的事件名称和事件处理名称会带上此后缀
        r.Environment = "Production";
        // 最大失败重试次数，默认60次
        r.RetryFailedMax = 1;
        // 消息重试间隔，默认2分钟
        r.RetryInterval = 1;
        // 单次重试消息数量限制，默认100
        r.RetryLimitCount = 100;
        // 成功的消息过期时间，默认3天，失败的消息永不过期，必须处理
        r.SucceedExpireHour = 24 * 3;
        // 消息处理失败后，重试器介入时间，默认5分钟后
        r.StartRetryAfter = 1;
        // 事务提交超时时间,单位秒,默认60秒
        r.TransactionCommitTimeout = 60;
        // 重试器执行时消息锁定时长
        r.LockTime = 110;
    })
    // 使用ef DbContext mysql
    .AddMySql<DataContext>()
    .AddMemoryQueue()
    .AddDashboard<TokenCookieAuth>()
    ;

var app = builder.Build();
using var serviceScope = app.Services.CreateScope();
var dataContext = serviceScope.ServiceProvider.GetRequiredService<DataContext>();
dataContext.Database.Migrate();

// Configure the HTTP request pipeline.

app.UseAuthorization();
app.UseRouting();
app.UseEventBusDashboard();

app.MapControllers();

app.Run();