using BenchmarkDotNet.Attributes;

namespace Hezium.Memory.Benchmarks;

public class AllocationBenchmarks
{
    [Params(1_048_576L, 4_294_967_296L)]
    public long Length { get; set; }

    [Benchmark]
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
