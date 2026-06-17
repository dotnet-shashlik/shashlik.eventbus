using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.Performance
{
    public class BenchmarkState
    {
        private long _publishedCount;
        private long _receivedCount;
        private long _expectedTotal;
        private readonly TaskCompletionSource<bool> _allReceived =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public long PublishedCount => Interlocked.Read(ref _publishedCount);

        public long ReceivedCount => Interlocked.Read(ref _receivedCount);

        public long ExpectedTotal => Interlocked.Read(ref _expectedTotal);

        public Task AllReceivedTask => _allReceived.Task;

        public void Reset(long expectedTotal)
        {
            Interlocked.Exchange(ref _publishedCount, 0);
            Interlocked.Exchange(ref _receivedCount, 0);
            Interlocked.Exchange(ref _expectedTotal, expectedTotal);
        }

        public void OnPublished()
        {
            Interlocked.Increment(ref _publishedCount);
        }

        public void OnReceived()
        {
            var current = Interlocked.Increment(ref _receivedCount);
            var expected = Interlocked.Read(ref _expectedTotal);
            if (expected > 0 && current >= expected)
            {
                _allReceived.TrySetResult(true);
            }
        }

        public void Abort(Exception ex)
        {
            _allReceived.TrySetException(ex);
        }
    }
}
