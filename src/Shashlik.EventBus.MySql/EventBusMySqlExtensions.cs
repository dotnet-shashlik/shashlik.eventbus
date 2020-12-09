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
        /// <param name="service"></param>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="publishTableName">已发布消息表名，默认eventbus_published</param>
        /// <param name="receiveTableName">已接收消息表名，默认eventbus_received</param>
        /// <returns></returns>
        public static IEventBusBuilder AddMySql(
            this IEventBusBuilder service,
            string connectionString,
            string? publishTableName = null,
            string? receiveTableName = null)
        {
            service.Services.Configure<EventBusMySqlOptions>(options =>
            {
                options.ConnectionString = connectionString;
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishedTableName = publishTableName!;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceivedTableName = receiveTableName!;
            });

            return service.AddMySql();
        }

        /// <summary>
        /// 使用DbContext注册mysql存储
        /// </summary>
        /// <param name="service"></param>
        /// <param name="publishTableName">已发布消息表名，默认eventbus_published</param>
        /// <param name="receiveTableName">已接收消息表名，默认eventbus_received</param>
        /// <typeparam name="TDbContext">数据库上下文类型</typeparam>
        /// <returns></returns>
        public static IEventBusBuilder AddMySql<TDbContext>(
            this IEventBusBuilder service,
            string? publishTableName = null,
            string? receiveTableName = null)
            where TDbContext : DbContext
        {
            service.Services.Configure<EventBusMySqlOptions>(options =>
            {
                options.DbContextType = typeof(TDbContext);
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishedTableName = publishTableName!;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceivedTableName = receiveTableName!;
            });

            return service.AddMySql();
        }

        /// <summary>
        /// 使用MySql存储
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public static IEventBusBuilder AddMySql(this IEventBusBuilder service)
        {
            service.Services.AddOptions<EventBusMySqlOptions>();
            service.Services.AddSingleton<IMessageStorage, MySqlMessageStorage>();
            service.Services.AddTransient<IMessageStorageInitializer, MySqlMessageStorageInitializer>();
            service.Services.AddSingleton<IConnectionString, DefaultConnectionString>();

            return service;
        }
    }
}