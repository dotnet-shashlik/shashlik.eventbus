namespace Shashlik.EventBus.RelationDbStorage;

/// <summary>
/// free sql 实例工厂,不注册到service,避免和应用应用free sql冲突
/// </summary>
public interface IFreeSqlFactory
{
    /// <summary>
    /// 需要保证为单例
    /// </summary>
    /// <returns></returns>
    public IFreeSql Instance();
}