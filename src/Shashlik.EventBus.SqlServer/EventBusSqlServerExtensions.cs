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
        /// <param name="service"></param>
        /// <returns></returns>
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