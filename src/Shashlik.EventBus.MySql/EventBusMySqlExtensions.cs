using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.MySql
{
    public static class EventBusMySqlExtensions
    {
        public static IEventBusBuilder AddMySql(
            this IEventBusBuilder service,
            string connectionString,
            string? publishTableName = null,
            string? receiveTableName = null)
        {
            service.Services.Configure<EventBusMySqlOptions>(options =>
            {
                options.ConnectionString = connectionString;
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishedTableName = publishTableName!;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceivedTableName = receiveTableName!;
            });

            return service.AddMySql();
        }

        public static IEventBusBuilder AddMySql<TDbContext>(
            this IEventBusBuilder service,
            string? publishTableName = null,
            string? receiveTableName = null)
            where TDbContext : DbContext
        {
            service.Services.Configure<EventBusMySqlOptions>(options =>
            {
                options.DbContextType = typeof(TDbContext);
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishedTableName = publishTableName!;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceivedTableName = receiveTableName!;
            });

            return service.AddMySql();
        }

        public static IEventBusBuilder AddMySql(this IEventBusBuilder service)
        {
            service.Services.AddOptions<EventBusMySqlOptions>();
            service.Services.AddSingleton<IMessageStorage, MySqlMessageStorage>();
            service.Services.AddTransient<IMessageStorageInitializer, MySqlMessageStorageInitializer>();
            service.Services.AddSingleton<IConnectionString, DefaultConnectionString>();

            return service;
        }
    }
}