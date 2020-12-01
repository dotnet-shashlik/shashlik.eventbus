﻿using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.EventBus.MemoryQueue;
using Shashlik.EventBus.PostgreSQL.Tests.Efcore;
using Shashlik.Kernel;

namespace Shashlik.EventBus.PostgreSQL.Tests
{
    public class TestStartup
    {
        public TestStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddControllers()
                .AddControllersAsServices();

            services.AddAuthentication();
            services.AddAuthorization();

            services.AddDbContextPool<DemoDbContext>(r =>
            {
                r.UseNpgsql(Configuration.GetConnectionString("Default"),
                    db => { db.MigrationsAssembly(typeof(DemoDbContext).Assembly.GetName().FullName); });
            }, 5);

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<DemoDbContext>();
            dbContext.Database.Migrate();
            
            services.AddEventBus(r =>
                {
                    r.Environment = TestBase.Env;
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
                .AddNpgsql<DemoDbContext>();

            services.AddShashlik(Configuration);
        }

        public void Configure(IApplicationBuilder app)
        {
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
    }
}