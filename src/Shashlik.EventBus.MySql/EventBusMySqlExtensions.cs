using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.MySql
{
    public static class EventBusMySqlExtensions
    {
        public static IServiceCollection AddMySql(
            this IServiceCollection service,
            string connectionString,
            string publishTableName = null,
            string receiveTableName = null)
        {
            service.Configure<EventBusMySqlOptions>(options =>
            {
                options.ConnectionString = connectionString;
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishTableName = publishTableName;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceiveTableName = receiveTableName;
            });

            return service.AddMySql();
        }

        public static IServiceCollection AddMySql<TDbContext>(
            this IServiceCollection service,
            string publishTableName = null,
            string receiveTableName = null)
            where TDbContext : DbContext
        {
            using var serviceProvider = service.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
            var connectionString = dbContext.Database.GetDbConnection().ConnectionString;

            service.Configure<EventBusMySqlOptions>(options =>
            {
                options.ConnectionString = connectionString;
                options.DbContextType = typeof(TDbContext);
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishTableName = publishTableName;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceiveTableName = receiveTableName;
            });

            return service.AddMySql();
        }

        public static IServiceCollection AddMySql(this IServiceCollection service)
        {
            service.AddOptions<EventBusMySqlOptions>();
            service.AddSingleton<IMessageStorage, MySqlMessageStorage>();
            service.AddTransient<IMessageStorageInitializer, MySqlMessageStorageInitializer>();

            return service;
        }
    }
}