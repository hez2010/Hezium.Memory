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
public class IndexedAccessBenchmarks
{
    private const int OperationCount = 8192;

    private JaggedArray<ushort> _loadJagged = null!;
    private JaggedArray<ushort> _storeJagged = null!;
    private BigArray<ushort> _loadBigArray = null!;
    private BigArray<ushort> _storeBigArray = null!;
    private long[] _indices = null!;

    [Params(1_048_576L, 4_294_967_296L)]
    public long Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _indices = BenchmarkHelpers.CreateRandomIndices(Length, OperationCount);

        _loadJagged = new JaggedArray<ushort>((nint)Length);
        _storeJagged = new JaggedArray<ushort>((nint)Length);
        BenchmarkHelpers.FillSequential(_loadJagged);

        _loadBigArray = new BigArray<ushort>((nint)Length);
        _storeBigArray = new BigArray<ushort>((nint)Length);
        BenchmarkHelpers.FillSequential(_loadBigArray.AsBigSpan());
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("RandomLoad")]
    public int JaggedRandomLoad()
    {
        int sum = 0;
        JaggedArray<ushort> jagged = _loadJagged;
        long[] indices = _indices;

        for (int i = 0; i < indices.Length; i++)
        {
            sum += jagged[(nint)indices[i]];
        }

        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("RandomLoad")]
    public int BigArrayRandomLoad()
    {
        int sum = 0;
        BigArray<ushort> array = _loadBigArray;
        long[] indices = _indices;

        for (int i = 0; i < indices.Length; i++)
        {
            sum += array[(nint)indices[i]];
        }

        return sum;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("RandomStore")]
    public int JaggedRandomStore()
    {
        int checksum = 0;
        JaggedArray<ushort> jagged = _storeJagged;
        long[] indices = _indices;

        for (int i = 0; i < indices.Length; i++)
        {
            ushort value = (ushort)(i + 1);
            jagged[(nint)indices[i]] = value;
            checksum += value;
        }

        return checksum;
    }

    [Benchmark]
    [BenchmarkCategory("RandomStore")]
    public int BigArrayRandomStore()
    {
        int checksum = 0;
        BigArray<ushort> array = _storeBigArray;
        long[] indices = _indices;

        for (int i = 0; i < indices.Length; i++)
        {
            ushort value = (ushort)(i + 1);
            array[(nint)indices[i]] = value;
            checksum += value;
        }

        return checksum;
    }

}
