using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.PostgreSQL
{
    public static class EventBusPostgreSQLExtensions
    {
        public static IServiceCollection AddEventBusPostgreSQLStorage(
            this IServiceCollection service,
            string connectionString,
            string publishTableName = null,
            string receiveTableName = null)
        {
            service.Configure<EventBusPostgreSQLOptions>(options =>
            {
                options.ConnectionString = connectionString;
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishTableName = publishTableName!;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceiveTableName = receiveTableName!;
            });

            return service.AddEventBusPostgreSQLStorage();
        }

        public static IServiceCollection AddEventBusPostgreSQLStorage<TDbContext>(
            this IServiceCollection service,
            string publishTableName = null,
            string receiveTableName = null)
            where TDbContext : DbContext
        {
            service.Configure<EventBusPostgreSQLOptions>(options =>
            {
                options.DbContextType = typeof(TDbContext);
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishTableName = publishTableName!;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceiveTableName = receiveTableName!;
            });

            return service.AddEventBusPostgreSQLStorage();
        }

        public static IServiceCollection AddEventBusPostgreSQLStorage(this IServiceCollection service)
        {
            service.AddOptions<EventBusPostgreSQLOptions>();
            service.AddSingleton<IMessageStorage, PostgreSQLMessageStorage>();
            service.AddTransient<IMessageStorageInitializer, PostgreSQLMessageSotrageInitializer>();
            service.AddSingleton<IConnectionString, DefaultConnectionString>();

            return service;
        }
    }
}
