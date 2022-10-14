using System;
using System.Threading;

namespace Shashlik.EventBus.DefaultImpl
{
    internal class InternalHostedStopToken : IHostedStopToken, IDisposable
    {
        public InternalHostedStopToken()
        {
            StopCancellationTokenSource = new CancellationTokenSource();
        }

        private CancellationTokenSource StopCancellationTokenSource { get; }
        public CancellationToken StopCancellationToken => StopCancellationTokenSource.Token;

        internal void Cancel()
        {
            StopCancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            StopCancellationTokenSource.Dispose();
        }
    }
}