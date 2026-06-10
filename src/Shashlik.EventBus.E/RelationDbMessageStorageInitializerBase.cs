using System.Threading;
using System.Threading.Tasks;

// ReSharper disable CheckNamespace

namespace Shashlik.EventBus.RelationDbStorage;

/// <summary>
/// 关系型数据库存储初始化器抽象基类。
/// 默认实现:通过 <see cref="IFreeSqlFactory"/> 获取 FreeSql 实例,
/// 调用 <c>CodeFirst.SyncStructure</c> 同步实体模型到物理表(自动建表 + 已声明索引)。
/// 子类可以重写 <see cref="InitializeAsync"/> 增加方言特定的 DDL/视图/种子数据等。
/// </summary>
internal class RelationDbMessageStorageInitializerBase : IMessageStorageInitializer
{
    protected RelationDbMessageStorageInitializerBase(IFreeSqlFactory freeSqlFactory)
    {
        FreeSqlFactory = freeSqlFactory;
    }

    /// <summary>
    /// FreeSql 实例工厂。各方言的 *FreeSqlFactory 实现负责在 Aop.ConfigEntity 里
    /// 设置表名/schema/charset 等方言相关配置;此处不再处理。
    /// </summary>
    protected IFreeSqlFactory FreeSqlFactory { get; }

    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var freeSql = FreeSqlFactory.Instance();
        // SyncStructure 仅在表不存在时创建,不会破坏已有表。
        // 表名/charset/schema 等由 IFreeSqlFactory.Aop.ConfigEntity 注入。
        freeSql.CodeFirst.SyncStructure(
            typeof(RelationDbMessageStoragePublishedModel),
            typeof(RelationDbMessageStorageReceivedModel));
        await Task.CompletedTask;
    }
}

/// <summary>
/// 默认的关系型数据库存储初始化器,直接使用基类行为。
/// 框架在 <see cref="RelationDbExtensions.AddRelationDb"/> 中默认注册此实现,
/// 应用方可继续注册自己的子类来扩展 DDL。
/// </summary>
internal class DefaultRelationDbMessageStorageInitializer : RelationDbMessageStorageInitializerBase
{
    public DefaultRelationDbMessageStorageInitializer(IFreeSqlFactory freeSqlFactory)
        : base(freeSqlFactory)
    {
    }
}