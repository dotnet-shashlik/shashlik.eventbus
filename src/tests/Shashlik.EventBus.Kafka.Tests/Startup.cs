using CommonTestLogical.EfCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.EventBus.MySql;
using Shashlik.Kernel;
using Shashlik.Utils.Extensions;

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
                var conn = Configuration.GetConnectionString("MySql");

                r.UseMySql(conn, ServerVersion.AutoDetect(conn),
                    db => { db.MigrationsAssembly(GetType().Assembly.GetName().FullName); });
            }, 5);

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DemoDbContext>();
            dbContext.Database.Migrate();

            services.AddEventBus(r =>
                {
                    var options = Configuration.GetSection("EventBus")
                        .Get<EventBusOptions>();
                    options.CopyTo(r);
                    r.Environment = _env;
                })
                .AddKafka(Configuration.GetSection("EventBus:Kafka"))
                .AddMySql<DemoDbContext>();

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