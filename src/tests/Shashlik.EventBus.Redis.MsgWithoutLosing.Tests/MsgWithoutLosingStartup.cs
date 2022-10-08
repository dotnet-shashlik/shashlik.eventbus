using CommonTestLogical.MsgWithoutLosing;
using FreeRedis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.EventBus.MemoryStorage;
using Shashlik.Kernel;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.Redis.MsgWithoutLosing.Tests
{
    public class MsgWithoutLosingStartup
    {
        public MsgWithoutLosingStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }
        private readonly string _env = CommonTestLogical.Utils.RandomEnv();

        public void ConfigureServices(IServiceCollection services)
        {
            var redisConn = Configuration.GetValue<string>("EventBus:Redis:Conn");
            services.AddSingleton(new RedisClient(redisConn));
            services.AddSingleton<IMessageListener, MsgWithoutLosingListener>();
            services.AddEventBus(r =>
                {
                    var options = Configuration.GetSection("EventBus")
                        .Get<EventBusOptions>();
                    options.CopyTo(r);
                    r.Environment = _env;
                })
                .AddRedisMQ()
                .AddMemoryStorage();

            services.AddShashlik(Configuration);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.ApplicationServices.UseShashlik()
                .AssembleServiceProvider()
                ;
        }
    }
}