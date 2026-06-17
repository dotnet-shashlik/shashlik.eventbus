namespace Sample.Performance
{
    public class BenchmarkOptions
    {
        public int Total { get; set; } = 5_000_000;

        public int Concurrency { get; set; }

        public int PayloadSize { get; set; } = 128;

        public int ConsumeTimeoutSeconds { get; set; } = 900;

        public int ProgressIntervalSeconds { get; set; } = 5;

        public string EnvironmentSuffix { get; set; } = "Perf";

        public int ResolvedConcurrency =>
            Concurrency > 0 ? Concurrency : System.Environment.ProcessorCount;
    }
}
