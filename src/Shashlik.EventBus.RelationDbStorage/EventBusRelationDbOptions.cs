using System;
using FreeSql;

namespace Shashlik.EventBus.RelationDbStorage;

/// <summary>
/// <see cref="Shashlik.EventBus.RelationDbStorage"/> 的入口选项。
/// 通过 <see cref="UseConnection"/> 指定 FreeSql 方言和连接串,
/// 框架会用对应的 FreeSql 方言 provider 跨方言地完成存储和读取。
/// <para>应用层 ORM 不受限制:EF Core / Dapper / NHibernate 等只要能拿到
/// <see cref="System.Data.IDbTransaction"/>,都可以包成
/// <see cref="ITransactionContext"/> 传进来以参与 EventBus 的事务。</para>
/// </summary>
public class EventBusRelationDbOptions
{
    /// <summary>
    /// FreeSql 方言(DataType.MySql / PostgreSQL / SqlServer / Sqlite / Oracle / Dameng ...)
    /// </summary>
    public DataType DataType { get; private set; }

    /// <summary>
    /// 数据库连接串
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// 已发布表名
    /// </summary>
    public string PublishedTableName { get; set; } = "eventbus_published";

    /// <summary>
    /// 已接收表名
    /// </summary>
    public string ReceivedTableName { get; set; } = "eventbus_received";

    /// <summary>
    /// 配置 FreeSql 方言
    /// </summary>
    public EventBusRelationDbOptions UseConnection(DataType dataType, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("connectionString cannot be empty", nameof(connectionString));
        DataType = dataType;
        ConnectionString = connectionString;
        return this;
    }
}
