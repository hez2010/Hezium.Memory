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
public class AllocationBenchmarks
{
    [Params(1_048_576L, 4_294_967_296L)]
    public long Length { get; set; }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Zeroed")]
    public JaggedArray<ushort> JaggedArray()
    {
        return new JaggedArray<ushort>((nint)Length);
    }

    [Benchmark]
    [BenchmarkCategory("Zeroed")]
    public BigArray<ushort> BigArray()
    {
        return new BigArray<ushort>((nint)Length);
    }
}
