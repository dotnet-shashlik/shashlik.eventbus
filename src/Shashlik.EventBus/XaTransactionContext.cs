using System.Transactions;

namespace Shashlik.EventBus
{
    /// <summary>
    /// xa transaction context,**Dispose后才能得到最新的状态**
    /// </summary>
    public class XaTransactionContext : ITransactionContext
    {
        public XaTransactionContext(Transaction original)
        {
            Information = original.TransactionInformation;
        }

        private TransactionInformation Information { get; }

        public bool IsDone()
        {
            return Information.Status == TransactionStatus.Aborted || Information.Status == TransactionStatus.Committed;
        }
    }
}