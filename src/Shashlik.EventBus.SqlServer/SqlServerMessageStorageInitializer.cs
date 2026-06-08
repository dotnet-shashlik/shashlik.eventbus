using Shashlik.EventBus.RelationDbStorage;

namespace Shashlik.EventBus.SqlServer;

public class SqlServerMessageStorageInitializer : RelationDbMessageStorageInitializerBase
{
    public SqlServerMessageStorageInitializer(IFreeSqlFactory freeSqlFactory) : base(freeSqlFactory)
    {
    }
}
