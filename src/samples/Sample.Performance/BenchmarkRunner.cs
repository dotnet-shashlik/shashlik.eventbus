using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shashlik.EventBus;

namespace Sample.Performance
{
    public class BenchmarkRunner : IHostedService
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly BenchmarkOptions _options;
        private readonly BenchmarkState _state;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly ILogger<BenchmarkRunner> _logger;

        private readonly CancellationTokenSource _cts = new();
        private Task? _runTask;

        public BenchmarkRunner(
            IEventPublisher eventPublisher,
            IOptions<BenchmarkOptions> options,
            BenchmarkState state,
            IHostApplicationLifetime appLifetime,
            ILogger<BenchmarkRunner> logger)
        {
            _eventPublisher = eventPublisher;
            _options = options.Value;
            _state = state;
            _appLifetime = appLifetime;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // 不阻塞 host 启动链,后台跑压测;完成后请求关闭进程
            _runTask = Task.Run(() => RunAsync(_cts.Token));
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            if (_runTask is not null)
            {
                try
                {
                    await Task.WhenAny(_runTask, Task.Delay(Timeout.Infinite, cancellationToken))
                        .ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                var total = _options.Total;
                var concurrency = _options.ResolvedConcurrency;
                var payload = BuildPayload(_options.PayloadSize);

                _state.Reset(total);

                PrintHeader(total, concurrency, payload.Length);

                // 启动进度打印
                using var progressCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var progressTask = ReportProgressAsync(progressCts.Token);

                // ============ Phase 1: Publish ============
                var publishSw = Stopwatch.StartNew();
                long nextIndex = 0;

                var workers = Enumerable.Range(0, concurrency).Select(_ => Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var idx = Interlocked.Increment(ref nextIndex) - 1;
                        if (idx >= total) break;

                        await _eventPublisher.PublishAsync(
                            new PerfEvent { Index = idx, Payload = payload },
                            null,
                            cancellationToken: cancellationToken).ConfigureAwait(false);

                        _state.OnPublished();
                    }
                }, cancellationToken)).ToArray();

                await Task.WhenAll(workers).ConfigureAwait(false);
                publishSw.Stop();

                // ============ Phase 2: Wait for consume ============
                var consumeSw = Stopwatch.StartNew();
                var consumeTimeout = TimeSpan.FromSeconds(_options.ConsumeTimeoutSeconds);
                var completed = await Task.WhenAny(
                    _state.AllReceivedTask,
                    Task.Delay(consumeTimeout, cancellationToken)).ConfigureAwait(false);
                consumeSw.Stop();

                progressCts.Cancel();
                try { await progressTask.ConfigureAwait(false); } catch { /* ignore */ }

                var consumedAll = completed == _state.AllReceivedTask;

                PrintResult(
                    total: total,
                    concurrency: concurrency,
                    payloadSize: payload.Length,
                    publishElapsed: publishSw.Elapsed,
                    consumeElapsed: consumeSw.Elapsed,
                    consumedAll: consumedAll);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Benchmark cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Benchmark failed");
            }
            finally
            {
                _appLifetime.StopApplication();
            }
        }

        private async Task ReportProgressAsync(CancellationToken cancellationToken)
        {
            var interval = TimeSpan.FromSeconds(Math.Max(1, _options.ProgressIntervalSeconds));
            var total = _options.Total;
            var lastPublished = 0L;
            var lastReceived = 0L;
            var lastTs = Stopwatch.GetTimestamp();

            while (!cancellationToken.IsCancellationRequested)
            {
                try { await Task.Delay(interval, cancellationToken).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }

                var nowTs = Stopwatch.GetTimestamp();
                var seconds = (nowTs - lastTs) / (double)Stopwatch.Frequency;
                var published = _state.PublishedCount;
                var received = _state.ReceivedCount;

                var publishRate = (published - lastPublished) / seconds;
                var receiveRate = (received - lastReceived) / seconds;

                lastPublished = published;
                lastReceived = received;
                lastTs = nowTs;

                Console.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss}] published={published:N0}/{total:N0} ({publishRate:N0}/s)  received={received:N0}/{total:N0} ({receiveRate:N0}/s)");
            }
        }

        private static string BuildPayload(int size)
        {
            if (size <= 0) return string.Empty;
            return new string('x', size);
        }

        private void PrintHeader(int total, int concurrency, int payloadSize)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("============== EventBus Performance Benchmark ==============");
            sb.AppendLine($"  Storage         : {Program.Storage}");
            sb.AppendLine($"  MQ              : {Program.MQ}");
            sb.AppendLine($"  Environment     : {Program.ResolvedEnvironment}");
            sb.AppendLine($"  Total messages  : {total:N0}");
            sb.AppendLine($"  Concurrency     : {concurrency}");
            sb.AppendLine($"  Payload size    : {payloadSize} B");
            sb.AppendLine($"  Started at      : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("============================================================");
            Console.WriteLine(sb.ToString());
        }

        private void PrintResult(
            int total,
            int concurrency,
            int payloadSize,
            TimeSpan publishElapsed,
            TimeSpan consumeElapsed,
            bool consumedAll)
        {
            var totalElapsed = publishElapsed + consumeElapsed;
            var receivedCount = _state.ReceivedCount;
            var publishTps = publishElapsed.TotalSeconds > 0 ? total / publishElapsed.TotalSeconds : 0;
            var consumeTps = consumeElapsed.TotalSeconds > 0 ? receivedCount / consumeElapsed.TotalSeconds : 0;
            var e2eTps = totalElapsed.TotalSeconds > 0 ? receivedCount / totalElapsed.TotalSeconds : 0;

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("================ Benchmark Result ================");
            sb.AppendLine($"  Storage          : {Program.Storage}");
            sb.AppendLine($"  MQ               : {Program.MQ}");
            sb.AppendLine($"  Total            : {total:N0}");
            sb.AppendLine($"  Concurrency      : {concurrency}");
            sb.AppendLine($"  Payload size     : {payloadSize} B");
            sb.AppendLine("--------------------------------------------------");
            sb.AppendLine($"  Publish duration : {Format(publishElapsed)}");
            sb.AppendLine($"  Publish TPS      : {publishTps:N0} msg/s");
            sb.AppendLine($"  Received         : {receivedCount:N0} / {total:N0}{(consumedAll ? "" : "  (TIMEOUT)")}");
            sb.AppendLine($"  Consume duration : {Format(consumeElapsed)}");
            sb.AppendLine($"  Consume TPS      : {consumeTps:N0} msg/s");
            sb.AppendLine($"  End-to-end       : {Format(totalElapsed)}");
            sb.AppendLine($"  End-to-end TPS   : {e2eTps:N0} msg/s");
            sb.AppendLine("==================================================");
            Console.WriteLine(sb.ToString());
        }

        private static string Format(TimeSpan ts)
        {
            return $"{ts:hh\\:mm\\:ss\\.fff} ({ts.TotalSeconds:N1}s)";
        }
    }
}
