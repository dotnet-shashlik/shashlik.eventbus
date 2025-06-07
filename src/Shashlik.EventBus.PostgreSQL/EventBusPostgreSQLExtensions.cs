using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.EventBus.RelationDbStorage;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.PostgreSQL
{
    public static class EventBusPostgreSQLExtensions
    {
        /// <summary>
        /// 使用连接字符串初始化注册PostgreSql存储
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="schema">模式，默认值eventbus</param>
        /// <param name="publishTableName">已发布数据表名，默认值published</param>
        /// <param name="receiveTableName">已接收数据表名，默认值received</param>
        /// <returns></returns>
        public static IEventBusBuilder AddNpgsql(
            this IEventBusBuilder eventBusBuilder,
            string connectionString,
            string? schema = null,
            string? publishTableName = null,
            string? receiveTableName = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionString));
            eventBusBuilder.Services.Configure<EventBusPostgreSQLOptions>(options =>
            {
                options.ConnectionString = connectionString;
                if (!schema.IsNullOrWhiteSpace())
                    options.Schema = schema!;
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishedTableName = publishTableName!;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceivedTableName = receiveTableName!;
            });

            return eventBusBuilder.AddNpgsqlCore();
        }

        /// <summary>
        /// 使用DbContext初始化注册PostgreSql存储
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="schema">模式，默认值eventbus</param>
        /// <param name="publishTableName">已发布数据表名，默认值published</param>
        /// <param name="receiveTableName">已接收数据表名，默认值received</param>
        /// <typeparam name="TDbContext"></typeparam>
        /// <returns></returns>
        public static IEventBusBuilder AddNpgsql<TDbContext>(
            this IEventBusBuilder eventBusBuilder,
            string? schema = null,
            string? publishTableName = null,
            string? receiveTableName = null)
            where TDbContext : DbContext
        {
            eventBusBuilder.Services.Configure<EventBusPostgreSQLOptions>(options =>
            {
                options.DbContextType = typeof(TDbContext);
                if (!schema.IsNullOrWhiteSpace())
                    options.Schema = schema!;
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishedTableName = publishTableName!;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceivedTableName = receiveTableName!;
            });

            return eventBusBuilder.AddNpgsqlCore();
        }

        /// <summary>
        /// 使用PostgreSql存储
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="optionsAction"></param>
        /// <returns></returns>
        public static IEventBusBuilder AddNpgsqlCore(this IEventBusBuilder eventBusBuilder, Action<EventBusPostgreSQLOptions>? optionsAction = null)
        {
            eventBusBuilder.Services.AddOptions<EventBusPostgreSQLOptions>();
            if (optionsAction != null)
                eventBusBuilder.Services.Configure(optionsAction);
            eventBusBuilder.Services.AddSingleton<IMessageStorage, PostgreSQLMessageStorage>();
            eventBusBuilder.Services.AddTransient<IMessageStorageInitializer, PostgreSQLMessageStorageInitializer>();
            eventBusBuilder.Services.AddSingleton<IConnectionString, DefaultConnectionString>();
            eventBusBuilder.Services.AddSingleton<IFreeSqlFactory, PostgreSQLFreeSqlFactory>();
            return eventBusBuilder;
        }
    }
}