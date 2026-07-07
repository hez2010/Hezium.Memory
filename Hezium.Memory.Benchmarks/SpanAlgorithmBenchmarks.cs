using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Hezium.Memory;

namespace Hezium.Memory.Benchmarks;

[MemoryDiagnoser]
[IterationCount(15)]
[IterationTime(100)]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[GcServer(true)]
public class SpanAlgorithmBenchmarks
{
    private JaggedArray<int> _sourceJagged = null!;
    private JaggedArray<int> _destinationJagged = null!;
    private BigArray<int> _sourceBigArray = null!;
    private BigArray<int> _destinationBigArray = null!;

    [Params(1_048_576L, 4_294_967_296L)]
    public long Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _sourceJagged = new JaggedArray<int>((nint)Length);
        _destinationJagged = new JaggedArray<int>((nint)Length);
        BenchmarkHelpers.FillSequential(_sourceJagged);
        BenchmarkHelpers.FillSequential(_destinationJagged);

        _sourceBigArray = new BigArray<int>((nint)Length);
        _destinationBigArray = new BigArray<int>((nint)Length);
        BenchmarkHelpers.FillSequential(_sourceBigArray.AsBigSpan());
        BenchmarkHelpers.FillSequential(_destinationBigArray.AsBigSpan());
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Fill")]
    public int JaggedFill()
    {
        JaggedArray<int> jagged = _destinationJagged;

        for (int i = 0; i < jagged.ChunkCount; i++)
        {
            jagged.GetChunkSpan(i).Fill(42);
        }

        return jagged[jagged.Length - 1];
    }

    [Benchmark]
    [BenchmarkCategory("Fill")]
    public int BigArrayFill()
    {
        _destinationBigArray.AsBigSpan().Fill(42);
        return _destinationBigArray[(nint)(Length - 1)];
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("CopyTo")]
    public int JaggedCopyTo()
    {
        JaggedArray<int> source = _sourceJagged;
        JaggedArray<int> destination = _destinationJagged;

        for (int i = 0; i < source.ChunkCount; i++)
        {
            source.GetChunkSpan(i).CopyTo(destination.GetChunkSpan(i));
        }

        return destination[destination.Length - 1];
    }

    [Benchmark]
    [BenchmarkCategory("CopyTo")]
    public int BigArrayCopyTo()
    {
        _sourceBigArray.AsBigSpan().CopyTo(_destinationBigArray.AsBigSpan());
        return _destinationBigArray[(nint)(Length - 1)];
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SequenceEqual")]
    public bool JaggedSequenceEqual()
    {
        JaggedArray<int> source = _sourceJagged;
        JaggedArray<int> destination = _destinationJagged;

        for (int i = 0; i < source.ChunkCount; i++)
        {
            if (!source.GetChunkSpan(i).SequenceEqual(destination.GetChunkSpan(i)))
            {
                return false;
            }
        }

        return true;
    }

    [Benchmark]
    [BenchmarkCategory("SequenceEqual")]
    public bool BigArraySequenceEqual()
    {
        return _sourceBigArray.AsBigSpan().SequenceEqual(_destinationBigArray.AsBigSpan());
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("BinarySearch")]
    public long JaggedBinarySearch()
    {
        long low = 0;
        long high = Length - 1;
        int target = BenchmarkHelpers.SearchValue;

        while (low <= high)
        {
            long index = low + ((high - low) >> 1);
            int current = _sourceJagged[(nint)index];

            if (current == target)
            {
                return index;
            }

            if (current < target)
            {
                low = index + 1;
            }
            else
            {
                high = index - 1;
            }
        }

        return ~low;
    }

    [Benchmark]
    [BenchmarkCategory("BinarySearch")]
    public nint BigArrayBinarySearch()
    {
        return _sourceBigArray.AsBigSpan().BinarySearch(BenchmarkHelpers.SearchValue);
    }

}
