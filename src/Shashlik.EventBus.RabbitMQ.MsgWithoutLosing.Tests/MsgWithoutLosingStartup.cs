using CommonTestLogical.MsgWithoutLosing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.EventBus.MemoryStorage;
using Shashlik.Kernel;

namespace Shashlik.EventBus.RabbitMQ.MsgWithoutLosing.Tests
{
    public class MsgWithoutLosingStartup
    {
        public MsgWithoutLosingStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        private static readonly string _env = CommonTestLogical.Utils.RandomEnv();

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IMessageListener, MsgWithoutLosingListener>();
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
                .AddRabbitMQ(Configuration.GetSection("EventBus:RabbitMQ"))
                .AddMemoryStorage();

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