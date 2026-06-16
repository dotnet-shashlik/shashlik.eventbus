using FreeSql;

namespace Shashlik.EventBus.RelationDbStorage;

public interface IConnectionFactory
{
    /// <summary>
    /// 数据库类型
    /// </summary>
    DataType DataType { get; }

    /// <summary>
    /// 连接字符串
    /// </summary>
    string ConnectionString { get; }
}