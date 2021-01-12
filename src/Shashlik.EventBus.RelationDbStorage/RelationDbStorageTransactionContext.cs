using System.Data;
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

namespace Shashlik.EventBus.RelationDbStorage
{
    public class RelationDbStorageTransactionContext : ITransactionContext
    {
        public RelationDbStorageTransactionContext(IDbTransaction dbTransaction)
        {
            DbTransaction = dbTransaction;
        }

        public IDbTransaction DbTransaction { get; }

        public virtual bool IsDone()
        {
            try
            {
                return DbTransaction.Connection is null;
            }
            catch
            {
                return true;
            }
        }
    }
}