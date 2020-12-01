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
        /// <summary>
        /// add npgsql services
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="connectionString"></param>
        /// <param name="publishTableName"></param>
        /// <param name="receiveTableName"></param>
        /// <returns></returns>
        public static IEventBusBuilder AddNpgsql(
            this IEventBusBuilder eventBusBuilder,
            string connectionString,
            string? publishTableName = null,
            string? receiveTableName = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionString));
            eventBusBuilder.Services.Configure<EventBusPostgreSQLOptions>(options =>
            {
                options.ConnectionString = connectionString;
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishedTableName = publishTableName!;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceivedTableName = receiveTableName!;
            });

            return eventBusBuilder.AddNpgsqlCore();
        }

        /// <summary>
        /// add npgsql services
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="publishTableName"></param>
        /// <param name="receiveTableName"></param>
        /// <typeparam name="TDbContext"></typeparam>
        /// <returns></returns>
        public static IEventBusBuilder AddNpgsql<TDbContext>(
            this IEventBusBuilder eventBusBuilder,
            string? publishTableName = null,
            string? receiveTableName = null)
            where TDbContext : DbContext
        {
            eventBusBuilder.Services.Configure<EventBusPostgreSQLOptions>(options =>
            {
                options.DbContextType = typeof(TDbContext);
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishedTableName = publishTableName!;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceivedTableName = receiveTableName!;
            });

            return eventBusBuilder.AddNpgsqlCore();
        }

        /// <summary>
        /// add npgsql core services
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <returns></returns>
        public static IEventBusBuilder AddNpgsqlCore(this IEventBusBuilder eventBusBuilder)
        {
            eventBusBuilder.Services.AddOptions<EventBusPostgreSQLOptions>();
            eventBusBuilder.Services.AddSingleton<IMessageStorage, PostgreSQLMessageStorage>();
            eventBusBuilder.Services.AddTransient<IMessageStorageInitializer, PostgreSQLMessageStorageInitializer>();
            eventBusBuilder.Services.AddSingleton<IConnectionString, DefaultConnectionString>();

            return eventBusBuilder;
        }
    }
}