using System.Data;

namespace Shashlik.EventBus.RelationDbStorage
{
    public class RelationDbStorageTransactionContext : ITransactionContext
    {
        public RelationDbStorageTransactionContext(IDbTransaction dbTransaction)
        {
            DbTransaction = dbTransaction;
        }

        public IDbTransaction DbTransaction { get; }
    }
}