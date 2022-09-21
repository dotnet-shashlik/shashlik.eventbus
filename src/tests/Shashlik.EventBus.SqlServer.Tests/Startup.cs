using CommonTestLogical;
using CommonTestLogical.EfCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.EventBus.MemoryQueue;
using Shashlik.Kernel;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.SqlServer.Tests
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
                r.UseSqlServer(Configuration.GetConnectionString("SqlServer"),
                    db => { db.MigrationsAssembly(GetType().Assembly.GetName().FullName); });
            }, 5);

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DemoDbContext>();
            dbContext.Database.Migrate();

            services.AddSingleton<IReceivedMessageRetryProvider, EmptyReceivedMessageRetryProvider>();
            services.AddSingleton<IPublishedMessageRetryProvider, EmptyPublishedMessageRetryProvider>();
            services.AddEventBus(r =>
                {
                    var options = Configuration.GetSection("EventBus")
                        .Get<EventBusOptions>();
                    options.CopyTo(r);
                    r.Environment = _env;
                })
                .AddMemoryQueue()
                .AddSqlServer<DemoDbContext>();

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