using Shashlik.EventBus.RelationDbStorage;

namespace Shashlik.EventBus.Sqlite;

public class SqliteMessageStorageInitializer : RelationDbMessageStorageInitializerBase
{
    public SqliteMessageStorageInitializer(IFreeSqlFactory freeSqlFactory) : base(freeSqlFactory)
    {
    }
}
