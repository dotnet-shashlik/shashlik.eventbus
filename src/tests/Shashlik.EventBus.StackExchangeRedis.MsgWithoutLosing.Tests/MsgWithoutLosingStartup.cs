using CommonTestLogical.MsgWithoutLosing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.EventBus.MemoryStorage;
using Shashlik.Kernel;
using Shashlik.Utils.Extensions;
using StackExchange.Redis;

namespace Shashlik.EventBus.StackExchangeRedis.MsgWithoutLosing.Tests
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
            var redisConn = Configuration.GetValue<string>("EventBus:StackExchangeRedis:Conn");
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));
            services.AddSingleton<IMessageListener, MsgWithoutLosingListener>();
            services.AddEventBus(r =>
                {
                    var options = Configuration.GetSection("EventBus")
                        .Get<EventBusOptions>();
                    options.CopyTo(r);
                    r.Environment = _env;
                })
                .AddStackExchangeRedisMQ()
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
