using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Hezium.Memory.Benchmarks;
using Perfolizer.Horology;

var config = DefaultConfig.Instance
    .AddDiagnoser(MemoryDiagnoser.Default)
    .AddJob(Job.Default
        .WithGcServer(true)
        .WithIterationCount(15)
        .WithIterationTime(TimeInterval.FromMilliseconds(100)))
    .AddJob(Job.Default
        .WithGcServer(false)
        .WithIterationCount(15)
        .WithIterationTime(TimeInterval.FromMilliseconds(100)));

BenchmarkRunner.Run<AllocationBenchmarks>(config, args);
BenchmarkRunner.Run<IndexedAccessBenchmarks>(config, args);
BenchmarkRunner.Run<SearchValuesBenchmarks>(config, args);
BenchmarkRunner.Run<SpanAlgorithmBenchmarks>(config, args);
