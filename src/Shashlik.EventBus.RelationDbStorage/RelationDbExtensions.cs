using System;
using System.Data;
using Microsoft.Extensions.DependencyInjection;

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
            eventBusBuilder.Services.AddOptions<EventBusRelationDbOptions>()
                .Configure(configure);
            eventBusBuilder.Services.AddSingleton<IFreeSqlFactory, DefaultFreeSqlFactory>();
            eventBusBuilder.Services.AddSingleton<IMessageStorage, RelationDbMessageStorage>();
            // 之前注册的是抽象基类 RelationDbMessageStorageInitializerBase,DI 容器无法
            // 解析抽象类型,导致启动时抛 ArgumentException。这里改成默认具体实现,
            // 应用方如果需要扩展 DDL,自己派生一个子类注册覆盖即可。
            eventBusBuilder.Services
                .AddTransient<IMessageStorageInitializer, DefaultRelationDbMessageStorageInitializer>();
            return eventBusBuilder;
        }

        /// <summary>
        /// 转换为EventBus 事务上下文
        /// </summary>
        /// <param name="relationDbTransactionContext"></param>
        /// <returns></returns>
        public static RelationDbStorageTransactionContext ToTransactionContext(
            this IDbTransaction relationDbTransactionContext)
        {
            return new RelationDbStorageTransactionContext(relationDbTransactionContext);
        }
    }
}