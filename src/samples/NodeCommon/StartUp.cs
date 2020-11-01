using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shashlik.EventBus;
using Shashlik.EventBus.MySql;
using Shashlik.EventBus.RabbitMQ;

namespace NodeCommon
{
    public class StartUp
    {
        public IServiceProvider Start(Action<IServiceCollection> action = null)
        {
            const string conn =
                "...";

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging(logging => { logging.AddConsole(); });

            serviceCollection.AddDbContextPool<DemoDbContext>(r =>
            {
                r.UseMySql(conn, db => { db.MigrationsAssembly(this.GetType().Assembly.GetName().FullName); });
            });

            serviceCollection.AddEventBus(r => { r.Environment = "demo"; })
                .AddMySql<DemoDbContext>()
                .AddRabbitMQ(r =>
                {
                    r.Host = "...";
                    r.UserName = "..";
                    r.Password = "...";
                })
                .Build();

            action?.Invoke(serviceCollection);

            return serviceCollection.BuildServiceProvider();
        }
    }
}