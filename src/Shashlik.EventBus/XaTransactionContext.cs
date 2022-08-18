using System;
using System.Transactions;

namespace Shashlik.EventBus
{
    /// <summary>
    /// xa transaction context(TransactionScope),**Dispose后才能得到最新的状态**
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
            return Information.Status is TransactionStatus.Aborted or TransactionStatus.Committed;
        }

        /// <summary>
        /// 获取当前xa事务上下文(TransactionScope)
        /// </summary>
        /// <returns></returns>
        public static ITransactionContext? Current
        {
            get
            {
                try
                {
                    if (Transaction.Current != null)
                        return new XaTransactionContext(Transaction.Current);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[EventBus] get xa transaction context occur error");
                    Console.WriteLine(ex.ToString());
                }

                return null;
            }
        }
    }
}