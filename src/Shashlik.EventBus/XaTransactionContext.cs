using System;
using System.Transactions;

namespace Shashlik.EventBus
{
    /// <summary>
    /// xa transaction context
    /// </summary>
    public class XaTransactionContext : ITransactionContext
    {
        public XaTransactionContext(Transaction current)
        {
            Original = current;
            current.TransactionCompleted += (s, e) => { _isDone = true; };
        }

        private Transaction Original { get; }
        private volatile bool _isDone;

        public bool IsDone()
        {
            try
            {
                if (_isDone)
                    return true;
                if (Original.TransactionInformation.Status == TransactionStatus.Aborted
                    || Original.TransactionInformation.Status == TransactionStatus.Committed)
                    return true;
            }
            catch (ObjectDisposedException)
            {
                // 事务对象已释放
                return true;
            }

            return false;
        }
    }
}