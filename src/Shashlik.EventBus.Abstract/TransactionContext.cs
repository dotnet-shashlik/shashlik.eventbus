// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global

namespace Shashlik.EventBus
{
    /// <summary>
    /// 当前事务上下文
    /// </summary>
    public class TransactionContext
    {
        public TransactionContext(object connectionInstance)
        {
            ConnectionInstance = connectionInstance;
        }
        
        public TransactionContext(object connectionInstance, object? transactionInstance)
        {
            ConnectionInstance = connectionInstance;
            TransactionInstance = transactionInstance;
        }

        /// <summary>
        /// 原始连接实例,一般是关系型数据库一般使用  IDbConnection/DbContext
        /// </summary>
        public object ConnectionInstance { get; }

        /// <summary>
        /// 原始事务实例,关系型数据库一般使用IDbContextTransaction/IDbTransaction
        /// </summary>
        public object? TransactionInstance { get; }
    }
}