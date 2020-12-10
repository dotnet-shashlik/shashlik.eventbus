using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shashlik.EventBus.MemoryQueue
{
    public class MemoryQueueHostedService : IHostedService
    {
        public MemoryQueueHostedService(ILoggerFactory loggerFactory, IHostedStopToken hostedStopToken)
        {
            LoggerFactory = loggerFactory;
            HostedStopToken = hostedStopToken;
        }

        private ILoggerFactory LoggerFactory { get; }
        private IHostedStopToken HostedStopToken { get; }

        public Task StartAsync(CancellationToken _)
        {
            InternalMemoryQueue.Start(LoggerFactory.CreateLogger(nameof(InternalMemoryQueue)), HostedStopToken.StopCancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken _)
        {
            return Task.CompletedTask;
        }
    }
}