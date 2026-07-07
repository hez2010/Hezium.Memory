using BenchmarkDotNet.Attributes;

namespace Hezium.Memory.Benchmarks;

public class IndexedAccessBenchmarks
{
    private const int OperationCount = 8192;

    private JaggedArray<int> _loadJagged = null!;
    private JaggedArray<int> _storeJagged = null!;
    private BigArray<int> _loadBigArray = null!;
    private BigArray<int> _storeBigArray = null!;
    private long[] _indices = null!;

    [Params(1_048_576L, 4_294_967_296L)]
    public long Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _indices = BenchmarkHelpers.CreateRandomIndices(Length, OperationCount);

        _loadJagged = new JaggedArray<int>((nint)Length);
        _storeJagged = new JaggedArray<int>((nint)Length);
        BenchmarkHelpers.FillSequential(_loadJagged);

        _loadBigArray = new BigArray<int>((nint)Length);
        _storeBigArray = new BigArray<int>((nint)Length);
        BenchmarkHelpers.FillSequential(_loadBigArray.AsBigSpan());
    }

    [Benchmark]
    [BenchmarkCategory("RandomLoad")]
    public int JaggedRandomLoad()
    {
        int sum = 0;
        JaggedArray<int> jagged = _loadJagged;
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
        BigArray<int> array = _loadBigArray;
        long[] indices = _indices;

        for (int i = 0; i < indices.Length; i++)
        {
            sum += array[(nint)indices[i]];
        }

        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("RandomStore")]
    public int JaggedRandomStore()
    {
        int checksum = 0;
        JaggedArray<int> jagged = _storeJagged;
        long[] indices = _indices;

        for (int i = 0; i < indices.Length; i++)
        {
            int value = i + 1;
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
        BigArray<int> array = _storeBigArray;
        long[] indices = _indices;

        for (int i = 0; i < indices.Length; i++)
        {
            int value = i + 1;
            array[(nint)indices[i]] = value;
            checksum += value;
        }

        return checksum;
    }
}
