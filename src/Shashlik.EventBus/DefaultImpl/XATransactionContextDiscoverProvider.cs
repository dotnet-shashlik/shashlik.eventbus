using Microsoft.Extensions.Logging;
using System;
using System.Transactions;

// ReSharper disable InconsistentNaming

namespace Shashlik.EventBus.DefaultImpl
{
    /// <summary>
    /// XA事务发现提供器,优先级10
    /// </summary>
    public class XATransactionContextDiscoverProvider : ITransactionContextDiscoverProvider
    {
        private ILogger<XATransactionContextDiscoverProvider> Logger { get; }

        public int Priority => 10;

        public XATransactionContextDiscoverProvider(ILogger<XATransactionContextDiscoverProvider> logger)
        {
            Logger = logger;
        }

        public ITransactionContext? Current()
        {
            try
            {
                if (Transaction.Current != null)
                    return new XaTransactionContext(Transaction.Current);
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "[EventBus] get xa transaction context occur error");
            }

            return null;
        }
    }
}