using System;
using System.Threading;
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
using Shashlik.Utils.Extensions;

namespace Node1
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
                            r.UseMySql(conn,
                                db =>
                                {
                                    db.MigrationsAssembly(typeof(DemoDbContext).Assembly.GetName().FullName);
                                });
                        });

                        services.AddEventBus(r => { r.Environment = "Demo"; })
                            .AddMySql<DemoDbContext>()
                            .AddRabbitMQ(r =>
                            {
                                r.Host = "frp1.jizhen.cool";
                                r.UserName = "rabbit";
                                r.Password = "8NnT2nUNoOwpBAue";
                            });

                        services.AddHostedService<TestService>();
                    })
                    .UseConsoleLifetime()
                    .Build();

            await host.RunAsync();
        }

        public class TestService : IHostedService
        {
            public TestService(IEventPublisher eventPublisher, DemoDbContext dbContext)
            {
                EventPublisher = eventPublisher;
                DbContext = dbContext;
            }

            private IEventPublisher EventPublisher { get; }
            private DemoDbContext DbContext { get; }
            public async Task StartAsync(CancellationToken cancellationToken)
            {
                for (int i = 0; i < 10; i++)
                {
                    var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);
                
                    await EventPublisher.PublishAsync(new Event1 { Name = $"张三: {i}" }, new TransactionContext(DbContext, transaction));

                    if (i == 2 || i == 4)
                    {
                        Console.WriteLine("rollback");
                        await transaction.RollbackAsync(cancellationToken);
                        continue;
                    }

                    await transaction.CommitAsync(cancellationToken);
                    await Task.Delay(1000, cancellationToken);
                }
                
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}