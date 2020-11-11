﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.PostgreSQL
{
    public static class EventBusPostgreSQLExtensions
    {
        public static IEventBusBuilder AddEventBusPostgreSQLStorage(
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

            return service.AddEventBusPostgreSQLStorage();
        }

        public static IEventBusBuilder AddEventBusPostgreSQLStorage<TDbContext>(
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

            return service.AddEventBusPostgreSQLStorage();
        }

        public static IEventBusBuilder AddEventBusPostgreSQLStorage(this IEventBusBuilder service)
        {
            service.Services.AddOptions<EventBusPostgreSQLOptions>();
            service.Services.AddSingleton<IMessageStorage, PostgreSQLMessageStorage>();
            service.Services.AddTransient<IMessageStorageInitializer, PostgreSqlMessageStorageInitializer>();
            service.Services.AddSingleton<IConnectionString, DefaultConnectionString>();

            return service;
        }
    }
}