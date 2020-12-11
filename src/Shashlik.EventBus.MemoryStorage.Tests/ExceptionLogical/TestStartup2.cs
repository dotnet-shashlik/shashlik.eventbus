using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.EventBus.MemoryQueue;
using Shashlik.Kernel;

namespace Shashlik.EventBus.MemoryStorage.Tests.ExceptionLogical
{
    public class TestStartup2
    {
        public TestStartup2(IConfiguration configuration)
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

            services.AddSingleton<IMessageListener, ExceptionMessageListener>();
            services.AddEventBus(r =>
                {
                    r.Environment = TestBase2.Env;
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
                .AddMemoryStorage();

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