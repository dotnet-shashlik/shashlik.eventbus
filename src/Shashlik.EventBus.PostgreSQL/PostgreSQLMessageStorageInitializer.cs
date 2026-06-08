using Shashlik.EventBus.RelationDbStorage;

namespace Shashlik.EventBus.PostgreSQL;

internal class PostgreSQLMessageStorageInitializer : RelationDbMessageStorageInitializerBase
{
    public PostgreSQLMessageStorageInitializer(IFreeSqlFactory freeSqlFactory) : base(freeSqlFactory)
    {
    }
}
