using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.Utils.Extensions;

// ReSharper disable AssignNullToNotNullAttribute

namespace Shashlik.EventBus.SqlServer
{
    public static class EventBusSqlServerExtensions
    {
        public static IEventBusBuilder AddSqlServer(
            this IEventBusBuilder service,
            string connectionString,
            string? publishTableName = null,
            string? receiveTableName = null)
        {
            service.Services.Configure<EventBusSqlServerOptions>(options =>
            {
                options.ConnectionString = connectionString;
                if (!publishTableName!.IsNullOrWhiteSpace())
                    options.PublishTableName = publishTableName!;
                if (!receiveTableName!.IsNullOrWhiteSpace())
                    options.ReceiveTableName = receiveTableName!;
            });

            return service.AddSqlServer();
        }

        public static IEventBusBuilder AddSqlServer<TDbContext>(
            this IEventBusBuilder service,
            string? publishTableName = null,
            string? receiveTableName = null)
            where TDbContext : DbContext
        {
            service.Services.Configure<EventBusSqlServerOptions>(options =>
            {
                options.DbContextType = typeof(TDbContext);
                if (!publishTableName!.IsNullOrWhiteSpace())
                    options.PublishTableName = publishTableName!;
                if (!receiveTableName!.IsNullOrWhiteSpace())
                    options.ReceiveTableName = receiveTableName!;
            });

            return service.AddSqlServer();
        }

        public static IEventBusBuilder AddSqlServer(this IEventBusBuilder service)
        {
            service.Services.AddOptions<EventBusSqlServerOptions>();
            service.Services.AddSingleton<IMessageStorage, SqlServerMessageStorage>();
            service.Services.AddTransient<IMessageStorageInitializer, SqlServerMessageStorageInitializer>();
            service.Services.AddSingleton<IConnectionString, DefaultConnectionString>();

            return service;
        }
    }
}