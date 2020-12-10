using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shashlik.EventBus.MemoryQueue
{
    public class MemoryQueueHostedService : IHostedService, IDisposable
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
            AppDomain.CurrentDomain.ProcessExit += (s, e) => StopCancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopCancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            try
            {
                StopCancellationTokenSource.Cancel();
                StopCancellationTokenSource.Dispose();
            }
            catch
            {
                // ignored
            }
        }
    }
}