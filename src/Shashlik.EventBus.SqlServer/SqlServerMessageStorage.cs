using Shashlik.EventBus.RelationDbStorage;

namespace Shashlik.EventBus.SqlServer
{
    public class SqlServerMessageStorage : RelationDbMessageStorageBase
    {
        public SqlServerMessageStorage(IFreeSqlFactory freeSqlFactory) : base(freeSqlFactory)
        {
        }
    }
}