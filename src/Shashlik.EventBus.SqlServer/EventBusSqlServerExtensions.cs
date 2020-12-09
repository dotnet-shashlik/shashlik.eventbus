using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.Utils.Extensions;

// ReSharper disable AssignNullToNotNullAttribute

namespace Shashlik.EventBus.SqlServer
{
    public static class EventBusSqlServerExtensions
    {
        /// <summary>
        /// 使用连接字符串初始化注册sqlserver存储
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="schema">模式，默认值eventbus</param>
        /// <param name="publishTableName">已发布数据表名，默认值published</param>
        /// <param name="receiveTableName">已接收数据表名，默认值received</param>
        /// <returns></returns>
        public static IEventBusBuilder AddSqlServer(
            this IEventBusBuilder eventBusBuilder,
            string connectionString,
            string? schema = null,
            string? publishTableName = null,
            string? receiveTableName = null)
        {
            eventBusBuilder.Services.Configure<EventBusSqlServerOptions>(options =>
            {
                options.ConnectionString = connectionString;
                if (!schema.IsNullOrWhiteSpace())
                    options.Schema = schema!;
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishedTableName = publishTableName!;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceivedTableName = receiveTableName!;
            });

            return eventBusBuilder.AddSqlServer();
        }

        /// <summary>
        /// 使用DbContext初始化注册sqlserver存储
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="schema">模式，默认值eventbus</param>
        /// <param name="publishTableName">已发布数据表名，默认值published</param>
        /// <param name="receiveTableName">已接收数据表名，默认值received</param>
        /// <typeparam name="TDbContext"></typeparam>
        /// <returns></returns>
        public static IEventBusBuilder AddSqlServer<TDbContext>(
            this IEventBusBuilder eventBusBuilder,
            string? schema = null,
            string? publishTableName = null,
            string? receiveTableName = null)
            where TDbContext : DbContext
        {
            eventBusBuilder.Services.Configure<EventBusSqlServerOptions>(options =>
            {
                options.DbContextType = typeof(TDbContext);
                if (!schema.IsNullOrWhiteSpace())
                    options.Schema = schema!;
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishedTableName = publishTableName!;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceivedTableName = receiveTableName!;
            });

            return eventBusBuilder.AddSqlServer();
        }

        /// <summary>
        /// 使用sqlserver存储
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="optionsAction"></param>
        /// <returns></returns>
        public static IEventBusBuilder AddSqlServer(this IEventBusBuilder eventBusBuilder, Action<EventBusSqlServerOptions>? optionsAction = null)
        {
            eventBusBuilder.Services.AddOptions<EventBusSqlServerOptions>();
            if (optionsAction != null)
                eventBusBuilder.Services.Configure(optionsAction);
            eventBusBuilder.Services.AddSingleton<IMessageStorage, SqlServerMessageStorage>();
            eventBusBuilder.Services.AddTransient<IMessageStorageInitializer, SqlServerMessageStorageInitializer>();
            eventBusBuilder.Services.AddSingleton<IConnectionString, DefaultConnectionString>();

            return eventBusBuilder;
        }
    }
}