using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Hezium.Memory.Benchmarks;

var config = DefaultConfig.Instance
    .AddDiagnoser(MemoryDiagnoser.Default)
    .AddJob(Job.Default
        .WithGcServer(true)
        .WithMaxIterationCount(30))
    .AddJob(Job.Default
        .WithGcServer(false)
        .WithMaxIterationCount(30));

BenchmarkRunner.Run<AllocationBenchmarks>(config, args);
BenchmarkRunner.Run<IndexedAccessBenchmarks>(config, args);
BenchmarkRunner.Run<SearchValuesBenchmarks>(config, args);
BenchmarkRunner.Run<SpanAlgorithmBenchmarks>(config, args);
