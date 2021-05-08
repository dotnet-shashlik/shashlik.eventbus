using CommonTestLogical.EfCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.EventBus.MemoryQueue;
using Shashlik.EventBus.MySql.Tests.Efcore;
using Shashlik.Kernel;

namespace Shashlik.EventBus.MySql.Tests
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }
        private readonly string _env = CommonTestLogical.Utils.RandomEnv();

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContextPool<DemoDbContext>(r =>
            {
                var conn = Configuration.GetConnectionString("Default");
                r.UseMySql(conn, ServerVersion.AutoDetect(conn),
                    db => { db.MigrationsAssembly(this.GetType().Assembly.GetName().FullName); });
            }, 5);

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DemoDbContext>();
            dbContext.Database.Migrate();

            services.AddEventBus(r =>
                {
                    r.Environment = _env;
                    // 为了便于测试，最大重试设置为7次
                    r.RetryFailedMax = 7;
                    // 重试开始工作的时间为2分钟后
                    r.StartRetryAfterSeconds = 2 * 60;
                    // 确认是否是否已提交时间为1分钟
                    r.ConfirmTransactionSeconds = 60;
                    // 失败重试间隔5秒
                    r.RetryIntervalSeconds = 5;
                })
                .AddMemoryQueue()
                .AddMySql<DemoDbContext>();

            services.AddShashlik(Configuration);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.ApplicationServices.UseShashlik()
                .AutowireServiceProvider()
                ;
        }
    }
}