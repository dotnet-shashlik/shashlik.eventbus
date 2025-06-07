using Shashlik.EventBus.RelationDbStorage;

namespace Shashlik.EventBus.Sqlite;

public class SqliteMessageStorage : RelationDbMessageStorageBase
{
    public SqliteMessageStorage(IFreeSqlFactory freeSqlFactory) : base(freeSqlFactory)
    {
    }
}