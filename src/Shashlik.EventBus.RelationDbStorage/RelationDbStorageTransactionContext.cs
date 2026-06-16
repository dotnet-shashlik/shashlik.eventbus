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
                // 1) 原实现:DbTransaction.Connection == null(Dispose 之后 native ADO.NET
                //    实现一般会清空引用)。
                var conn = DbTransaction.Connection;
                if (conn is null) return true;
                // 2) EF Core 走 RelationalTransaction 包装,Commit/Rollback 之后底层
                //    IDbTransaction 引用通常仍然非空,但 Connection.State 经常是 Closed
                //    (尤其用户用 using scope 包住了 DbContext 的情况下)。Closed/Broken
                //    视为事务已终结。
                if (conn.State == ConnectionState.Closed || conn.State == ConnectionState.Broken)
                    return true;
                return false;
            }
            catch
            {
                return true;
            }
        }
    }
}