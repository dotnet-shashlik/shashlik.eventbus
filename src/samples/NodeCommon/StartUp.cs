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
                "server=frp1.jizhen.cool;database=sbt;user=root;password=jizhen.cool.0416;Pooling=True;Min Pool Size=3;Max Pool Size=5;";

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging(logging => { logging.AddConsole(); });

            serviceCollection.AddDbContextPool<DemoDbContext>(r =>
            {
                r.UseMySql(conn, db => { db.MigrationsAssembly(this.GetType().Assembly.GetName().FullName); });
            });

            serviceCollection.AddEventBus(r => { r.Environment = "Demo"; })
                .AddMySql<DemoDbContext>()
                .AddRabbitMQ(r =>
                {
                    r.Host = "frp1.jizhen.cool";
                    r.UserName = "rabbit";
                    r.Password = "8NnT2nUNoOwpBAue";
                });

            action?.Invoke(serviceCollection);

            return serviceCollection.BuildServiceProvider();
        }
    }
}