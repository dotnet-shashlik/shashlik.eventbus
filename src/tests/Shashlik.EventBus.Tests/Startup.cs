using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.EventBus.MemoryQueue;
using Shashlik.EventBus.MemoryStorage;
using Shashlik.Kernel;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.Tests
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
            services.AddEventBus(r =>
                {
                    var options = Configuration.GetSection("EventBus")
                        .Get<EventBusOptions>();
                    options.CopyTo(r);
                    r.Environment = _env;
                })
                .AddMemoryQueue()
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