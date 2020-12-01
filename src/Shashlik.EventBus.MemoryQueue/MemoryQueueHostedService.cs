using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shashlik.EventBus.MemoryQueue
{
    public class MemoryQueueHostedService : IHostedService
    {
        public MemoryQueueHostedService(ILoggerFactory loggerFactory)
        {
            StopCancellationTokenSource = new CancellationTokenSource();
            LoggerFactory = loggerFactory;
        }

        private ILoggerFactory LoggerFactory { get; }
        private CancellationTokenSource StopCancellationTokenSource { get; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            InternalQueue.Start(LoggerFactory.CreateLogger(typeof(InternalQueue)), StopCancellationTokenSource.Token);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopCancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }
    }
}