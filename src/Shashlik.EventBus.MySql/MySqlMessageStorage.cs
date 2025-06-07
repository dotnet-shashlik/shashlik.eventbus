using Shashlik.EventBus.RelationDbStorage;

namespace Shashlik.EventBus.MySql;

public class MySqlMessageStorage : RelationDbMessageStorageBase
{
    public MySqlMessageStorage(IFreeSqlFactory freeSqlFactory) : base(freeSqlFactory)
    {
    }
}