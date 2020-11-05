using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.Utils.Extensions;

// ReSharper disable AssignNullToNotNullAttribute

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
            service.Configure<EventBusMySqlOptions>(options =>
            {
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
            service.AddSingleton<IConnectionString, DefaultConnectionString>();

            return service;
        }
    }
}