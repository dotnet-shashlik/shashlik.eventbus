using System;
using FreeSql;

namespace Shashlik.EventBus.Extensions.EfCore;

public class EventBusEfCoreOptions
{
    /// <summary>
    /// 数据库类型, 为空时自动从DbContext中推断
    /// </summary>
    public DataType DataType { get; set; }

    /// <summary>
    /// 数据库上下文类型
    /// </summary>
    public Type DbContextType { get; set; } = null!;
}