using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodeCommon;
using Shashlik.EventBus;

namespace Node1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var ser = new StartUp()
                .Start(serviceCollection => serviceCollection.AddTransient<TestEventHandler1>());

            var eventPublisher = ser.GetService<IEventPublisher>();

            var dbContext = ser.GetService<DemoDbContext>();
            var transaction = await dbContext.Database.BeginTransactionAsync();

            await eventPublisher.PublishAsync(new Event1 { Name = "张三" }, new TransactionContext(dbContext, transaction));

            await transaction.CommitAsync();

            Console.ReadLine();
        }
    }
}