using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Hezium.Memory.Benchmarks;

var config = new ConfigGC();
BenchmarkRunner.Run<AllocationBenchmarks>(config, args);
BenchmarkRunner.Run<IndexedAccessBenchmarks>(config, args);
BenchmarkRunner.Run<SearchValuesBenchmarks>(config, args);
BenchmarkRunner.Run<SpanAlgorithmBenchmarks>(config, args);

class ConfigGC : ManualConfig
{
    public ConfigGC()
    {
        AddJob(Job.Default.WithGcServer(true).WithId("ServerGC"));
        AddJob(Job.Default.WithGcServer(false).WithId("WorkstationGC"));
    }
}
