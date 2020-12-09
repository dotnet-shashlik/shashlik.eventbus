using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.MySql
{
    public static class EventBusMySqlExtensions
    {
        /// <summary>
        /// 使用连接字符串初始化注册mysql存储
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="publishTableName">已发布消息表名，默认eventbus_published</param>
        /// <param name="receiveTableName">已接收消息表名，默认eventbus_received</param>
        /// <returns></returns>
        public static IEventBusBuilder AddMySql(
            this IEventBusBuilder eventBusBuilder,
            string connectionString,
            string? publishTableName = null,
            string? receiveTableName = null)
        {
            eventBusBuilder.Services.Configure<EventBusMySqlOptions>(options =>
            {
                options.ConnectionString = connectionString;
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishedTableName = publishTableName!;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceivedTableName = receiveTableName!;
            });

            return eventBusBuilder.AddMySql();
        }

        /// <summary>
        /// 使用DbContext注册mysql存储
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="publishTableName">已发布消息表名，默认eventbus_published</param>
        /// <param name="receiveTableName">已接收消息表名，默认eventbus_received</param>
        /// <typeparam name="TDbContext">数据库上下文类型</typeparam>
        /// <returns></returns>
        public static IEventBusBuilder AddMySql<TDbContext>(
            this IEventBusBuilder eventBusBuilder,
            string? publishTableName = null,
            string? receiveTableName = null)
            where TDbContext : DbContext
        {
            eventBusBuilder.Services.Configure<EventBusMySqlOptions>(options =>
            {
                options.DbContextType = typeof(TDbContext);
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishedTableName = publishTableName!;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceivedTableName = receiveTableName!;
            });

            return eventBusBuilder.AddMySql();
        }

        /// <summary>
        /// 使用MySql存储
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="optionsAction"></param>
        /// <returns></returns>
        public static IEventBusBuilder AddMySql(this IEventBusBuilder eventBusBuilder, Action<EventBusMySqlOptions>? optionsAction = null)
        {
            eventBusBuilder.Services.AddOptions<EventBusMySqlOptions>();
            if (optionsAction != null)
                eventBusBuilder.Services.Configure(optionsAction);
            eventBusBuilder.Services.AddSingleton<IMessageStorage, MySqlMessageStorage>();
            eventBusBuilder.Services.AddTransient<IMessageStorageInitializer, MySqlMessageStorageInitializer>();
            eventBusBuilder.Services.AddSingleton<IConnectionString, DefaultConnectionString>();

            return eventBusBuilder;
        }
    }
}