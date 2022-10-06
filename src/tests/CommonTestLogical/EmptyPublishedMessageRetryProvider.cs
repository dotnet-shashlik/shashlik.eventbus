using System.Threading;
using System.Threading.Tasks;
using Shashlik.EventBus;

namespace CommonTestLogical
{
    public class EmptyPublishedMessageRetryProvider : IPublishedMessageRetryProvider
    {
        public Task StartupAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<HandleResult> RetryAsync(string id, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HandleResult(true));
        }
    }
}