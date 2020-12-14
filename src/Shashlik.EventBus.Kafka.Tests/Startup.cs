using System;
using CommonTestLogical.EfCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Shashlik.EventBus.Kafka.Tests.Efcore;
using Shashlik.EventBus.MySql;
using Shashlik.Kernel;

namespace Shashlik.EventBus.Kafka.Tests
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
            services.AddMemoryCache();
            services.AddControllers()
                .AddControllersAsServices();

            services.AddAuthentication();
            services.AddAuthorization();
            services.AddDbContextPool<DemoDbContext>(r =>
            {
                r.UseMySql(Configuration.GetConnectionString("Default"), ServerVersion.FromString("5.7"),
                    db => { db.MigrationsAssembly(GetType().Assembly.GetName().FullName); });
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
                    // 确认事务是否已提交时间为1分钟
                    r.ConfirmTransactionSeconds = 60;
                    // 失败重试间隔5秒
                    r.RetryIntervalSeconds = 5;
                })
                .AddKafka(Configuration.GetSection("EventBus:Kafka"))
                .AddMySql<DemoDbContext>();

            services.AddShashlik(Configuration);
        }

        public void Configure(IApplicationBuilder app)
        {
            ClearTestData(app.ApplicationServices);
            app.ApplicationServices.UseShashlik()
                .AutowireServiceProvider()
                ;


            // mvc
            app.UseRouting();

            app.UseStaticFiles();

            // 认证
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapDefaultControllerRoute(); });
        }

        /// <summary>
        /// 清理测试数据
        /// </summary>
        private void ClearTestData(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<IOptions<EventBusMySqlOptions>>().Value;
            using var conn = new MySqlConnection(Configuration.GetConnectionString("Default"));
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
delete from {options.PublishedTableName};
delete from {options.ReceivedTableName};
";
            cmd.ExecuteNonQuery();
        }
    }
}