using System;
using Microsoft.Extensions.DependencyInjection;
using Shashlik.EventBus.DefaultImpl;

namespace Shashlik.EventBus.RelationDbStorage
{
    public static class RelationDbExtensions
    {
        /// <summary>
        /// 使用 FreeSql 跨方言实现关系型数据库存储(MySQL/PG/SqlServer/Sqlite/...)。
        /// 应用层 ORM 自由(EF Core / Dapper / NHibernate 都能用),
        /// 只要能拿到 <see cref="System.Data.IDbTransaction"/> 即可包装成
        /// <see cref="ITransactionContext"/> 参与 EventBus 事务。
        /// </summary>
        public static IEventBusBuilder AddRelationDb(
            this IEventBusBuilder eventBusBuilder,
            Action<EventBusRelationDbOptions> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            var options = new EventBusRelationDbOptions();
            configure(options);
            if (options.DataType == default || string.IsNullOrWhiteSpace(options.ConnectionString))
                throw new InvalidOperationException(
                    "[EventBus-RelationDb] must call UseConnection(dataType, connectionString) in configure");

            eventBusBuilder.Services.AddOptions<EventBusRelationDbOptions>()
                .Configure(o =>
                {
                    o.UseConnection(options.DataType, options.ConnectionString);
                    o.PublishedTableName = options.PublishedTableName;
                    o.ReceivedTableName = options.ReceivedTableName;
                });
            eventBusBuilder.Services.AddSingleton<IFreeSqlFactory, DefaultFreeSqlFactory>();
            eventBusBuilder.Services.AddSingleton<IMessageStorage, RelationDbMessageStorage>();
            eventBusBuilder.Services.AddTransient<IMessageStorageInitializer, RelationDbMessageStorageInitializerBase>();
            return eventBusBuilder;
        }
    }
}
