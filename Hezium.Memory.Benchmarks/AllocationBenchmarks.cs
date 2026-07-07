using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;

namespace Hezium.Memory.Benchmarks;

[MemoryDiagnoser]
[IterationCount(15)]
[IterationTime(100)]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[GcServer(true)]
public class AllocationBenchmarks
{
    [Params(1_048_576L, 4_294_967_296L)]
    public long Length { get; set; }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Zeroed")]
    public JaggedArray<int> JaggedArray()
    {
        return new JaggedArray<int>((nint)Length);
    }

    [Benchmark]
    [BenchmarkCategory("Zeroed")]
    public BigArray<int> BigArray()
    {
        return new BigArray<int>((nint)Length);
    }
}
