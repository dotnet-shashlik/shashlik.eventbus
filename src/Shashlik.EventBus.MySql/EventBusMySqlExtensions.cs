using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.MySql
{
    public static class EventBusMySqlExtensions
    {
        public static IEventBusBuilder AddMySql(
            this IEventBusBuilder builder,
            string connectionString,
            string publishTableName = null,
            string receiveTableName = null)
        {
            builder.ServiceCollection.Configure<EventBusMySqlOptions>(options =>
            {
                options.ConnectionString = connectionString;
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishTableName = publishTableName;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceiveTableName = receiveTableName;
            });

            return builder.AddMySql();
        }

        public static IEventBusBuilder AddMySql<TDbContext>(
            this IEventBusBuilder builder,
            string publishTableName = null,
            string receiveTableName = null)
            where TDbContext : DbContext
        {
            using var serviceProvider = builder.ServiceCollection.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
            var connectionString = dbContext.Database.GetDbConnection().ConnectionString;

            builder.ServiceCollection.Configure<EventBusMySqlOptions>(options =>
            {
                options.ConnectionString = connectionString;
                options.DbContextType = typeof(TDbContext);
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishTableName = publishTableName;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceiveTableName = receiveTableName;
            });

            return builder.AddMySql();
        }

        public static IEventBusBuilder AddMySql(this IEventBusBuilder builder)
        {
            builder.ServiceCollection.AddOptions<EventBusMySqlOptions>();
            builder.ServiceCollection.AddSingleton<IMessageStorage, MySqlMessageStorage>();
            builder.ServiceCollection.AddTransient<IMessageStorageInitializer, MySqlMessageStorageInitializer>();

            return builder;
        }
    }
}