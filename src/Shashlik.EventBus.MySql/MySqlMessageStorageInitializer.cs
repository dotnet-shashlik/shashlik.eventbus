using Shashlik.EventBus.RelationDbStorage;

namespace Shashlik.EventBus.MySql;

public class MySqlMessageStorageInitializer : RelationDbMessageStorageInitializerBase
{
    public MySqlMessageStorageInitializer(IFreeSqlFactory freeSqlFactory) : base(freeSqlFactory)
    {
    }
}
