using System.Threading;

namespace Shashlik.EventBus.DefaultImpl
{
    internal class InternalHostedStopToken : IHostedStopToken
    {
        public InternalHostedStopToken()
        {
            StopCancellationTokenSource = new CancellationTokenSource();
        }

        private CancellationTokenSource StopCancellationTokenSource { get; }
        public CancellationToken StopCancellationToken => StopCancellationTokenSource.Token;

        public void Cancel()
        {
            StopCancellationTokenSource.Cancel();
        }
    }
}