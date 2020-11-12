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
        public static IEventBusBuilder AddNpgsql(
            this IEventBusBuilder service,
            string connectionString,
            string publishTableName = null,
            string receiveTableName = null)
        {
            service.Services.Configure<EventBusPostgreSQLOptions>(options =>
            {
                options.ConnectionString = connectionString;
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishTableName = publishTableName!;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceiveTableName = receiveTableName!;
            });

            return service.AddNpgsql();
        }

        public static IEventBusBuilder AddNpgsql<TDbContext>(
            this IEventBusBuilder service,
            string publishTableName = null,
            string receiveTableName = null)
            where TDbContext : DbContext
        {
            service.Services.Configure<EventBusPostgreSQLOptions>(options =>
            {
                options.DbContextType = typeof(TDbContext);
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishTableName = publishTableName!;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceiveTableName = receiveTableName!;
            });

            return service.AddNpgsql();
        }

        public static IEventBusBuilder AddNpgsql(this IEventBusBuilder service)
        {
            service.Services.AddOptions<EventBusPostgreSQLOptions>();
            service.Services.AddSingleton<IMessageStorage, PostgreSQLMessageStorage>();
            service.Services.AddTransient<IMessageStorageInitializer, PostgreSQLMessageStorageInitializer>();
            service.Services.AddSingleton<IConnectionString, DefaultConnectionString>();

            return service;
        }
    }
}