using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.Sqlite
{
    public static class EventBusSqliteExtensions
    {
        /// <summary>
        /// 使用连接字符串初始化注册sqlite存储
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="publishTableName">已发布消息表名，默认eventbus_published</param>
        /// <param name="receiveTableName">已接收消息表名，默认eventbus_received</param>
        /// <returns></returns>
        public static IEventBusBuilder AddSqlite(
            this IEventBusBuilder eventBusBuilder,
            string connectionString,
            string? publishTableName = null,
            string? receiveTableName = null)
        {
            eventBusBuilder.Services.Configure<EventBusSqliteOptions>(options =>
            {
                options.ConnectionString = connectionString;
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishedTableName = publishTableName!;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceivedTableName = receiveTableName!;
            });

            return eventBusBuilder.AddSqlite();
        }

        /// <summary>
        /// 使用DbContext注册sqlite存储
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="publishTableName">已发布消息表名，默认eventbus_published</param>
        /// <param name="receiveTableName">已接收消息表名，默认eventbus_received</param>
        /// <typeparam name="TDbContext">数据库上下文类型</typeparam>
        /// <returns></returns>
        public static IEventBusBuilder AddSqlite<TDbContext>(
            this IEventBusBuilder eventBusBuilder,
            string? publishTableName = null,
            string? receiveTableName = null)
            where TDbContext : DbContext
        {
            eventBusBuilder.Services.Configure<EventBusSqliteOptions>(options =>
            {
                options.DbContextType = typeof(TDbContext);
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishedTableName = publishTableName!;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceivedTableName = receiveTableName!;
            });

            return eventBusBuilder.AddSqlite();
        }

        /// <summary>
        /// 使用Sqlite存储
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="optionsAction"></param>
        /// <returns></returns>
        public static IEventBusBuilder AddSqlite(this IEventBusBuilder eventBusBuilder, Action<EventBusSqliteOptions>? optionsAction = null)
        {
            eventBusBuilder.Services.AddOptions<EventBusSqliteOptions>();
            if (optionsAction != null)
                eventBusBuilder.Services.Configure(optionsAction);
            eventBusBuilder.Services.AddSingleton<IMessageStorage, SqliteMessageStorage>();
            eventBusBuilder.Services.AddTransient<IMessageStorageInitializer, SqliteMessageStorageInitializer>();
            eventBusBuilder.Services.AddSingleton<IConnectionString, DefaultConnectionString>();

            return eventBusBuilder;
        }
    }
}