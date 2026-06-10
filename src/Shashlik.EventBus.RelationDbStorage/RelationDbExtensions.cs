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
            // 注意:DataType.MySql == 0 == default(DataType),所以不能简单判 "== default",
            // 否则 MySql 永远校验失败。改成:只要没显式调用 UseConnection,就报错。
            if (!options.IsConfigured)
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
            // 之前注册的是抽象基类 RelationDbMessageStorageInitializerBase,DI 容器无法
            // 解析抽象类型,导致启动时抛 ArgumentException。这里改成默认具体实现,
            // 应用方如果需要扩展 DDL,自己派生一个子类注册覆盖即可。
            eventBusBuilder.Services.AddTransient<IMessageStorageInitializer, DefaultRelationDbMessageStorageInitializer>();
            return eventBusBuilder;
        }
    }
}
