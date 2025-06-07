using Shashlik.EventBus.RelationDbStorage;

namespace Shashlik.EventBus.PostgreSQL;

public class PostgreSQLMessageStorage : RelationDbMessageStorageBase
{
    public PostgreSQLMessageStorage(IFreeSqlFactory freeSqlFactory) : base(freeSqlFactory)
    {
    }
}