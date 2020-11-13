using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.Utils.Extensions;

namespace Shashlik.EventBus.MySql
{
    public static class EventBusMySqlExtensions
    {
        /// <summary>
        /// add mysql services
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="connectionString"></param>
        /// <param name="publishTableName"></param>
        /// <param name="receiveTableName"></param>
        /// <returns></returns>
        public static IEventBusBuilder AddMySql(
            this IEventBusBuilder eventBusBuilder,
            string connectionString,
            string publishTableName = null,
            string receiveTableName = null)
        {
            eventBusBuilder.Services.Configure<EventBusMySqlOptions>(options =>
            {
                options.ConnectionString = connectionString;
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishTableName = publishTableName!;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceiveTableName = receiveTableName!;
            });

            return eventBusBuilder.AddMySqlCore();
        }

        /// <summary>
        /// add mysql services
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <param name="publishTableName"></param>
        /// <param name="receiveTableName"></param>
        /// <typeparam name="TDbContext"></typeparam>
        /// <returns></returns>
        public static IEventBusBuilder AddMySql<TDbContext>(
            this IEventBusBuilder eventBusBuilder,
            string publishTableName = null,
            string receiveTableName = null)
            where TDbContext : DbContext
        {
            eventBusBuilder.Services.Configure<EventBusMySqlOptions>(options =>
            {
                options.DbContextType = typeof(TDbContext);
                if (!publishTableName.IsNullOrWhiteSpace())
                    options.PublishTableName = publishTableName!;
                if (!receiveTableName.IsNullOrWhiteSpace())
                    options.ReceiveTableName = receiveTableName!;
            });

            return eventBusBuilder.AddMySqlCore();
        }

        /// <summary>
        /// add mysql core services
        /// </summary>
        /// <param name="eventBusBuilder"></param>
        /// <returns></returns>
        public static IEventBusBuilder AddMySqlCore(this IEventBusBuilder eventBusBuilder)
        {
            eventBusBuilder.Services.AddOptions<EventBusMySqlOptions>();
            eventBusBuilder.Services.AddSingleton<IMessageStorage, MySqlMessageStorage>();
            eventBusBuilder.Services.AddTransient<IMessageStorageInitializer, MySqlMessageStorageInitializer>();
            eventBusBuilder.Services.AddSingleton<IConnectionString, DefaultConnectionString>();

            return eventBusBuilder;
        }
    }
}