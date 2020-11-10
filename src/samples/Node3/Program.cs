using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodeCommon;
using Shashlik.EventBus;
using Shashlik.EventBus.MySql;
using Shashlik.EventBus.RabbitMQ;

namespace Node3
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder().ConfigureHostConfiguration(configHost => { configHost.AddCommandLine(args); })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<TestEventHandler1>();
                    const string conn =
                        "...";

                    services.AddLogging(logging => { logging.AddConsole(); });

                    services.AddDbContextPool<DemoDbContext>(r =>
                    {
                        r.UseNpgsql(conn,
                            db =>
                            {
                                db.MigrationsAssembly(typeof(DemoDbContext).Assembly.GetName().FullName);
                            });
                    });

                    services.AddEventBus(r => { r.Environment = "Demo"; })
                        .AddMySql<DemoDbContext>()
                        .AddRabbitMQ(r =>
                        {
                            r.Host = "...";
                            r.UserName = "...";
                            r.Password = "...";
                        });
                })
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();
        }
    }
}