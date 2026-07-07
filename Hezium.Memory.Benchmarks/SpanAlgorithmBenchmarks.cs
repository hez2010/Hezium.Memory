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
public class SpanAlgorithmBenchmarks
{
    private JaggedArray<ushort> _sourceJagged = null!;
    private JaggedArray<ushort> _destinationJagged = null!;
    private BigArray<ushort> _sourceBigArray = null!;
    private BigArray<ushort> _destinationBigArray = null!;

    [Params(1_048_576L, 4_294_967_296L)]
    public long Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _sourceJagged = new JaggedArray<ushort>((nint)Length);
        _destinationJagged = new JaggedArray<ushort>((nint)Length);
        BenchmarkHelpers.FillSequential(_sourceJagged);
        BenchmarkHelpers.FillSequential(_destinationJagged);

        _sourceBigArray = new BigArray<ushort>((nint)Length);
        _destinationBigArray = new BigArray<ushort>((nint)Length);
        BenchmarkHelpers.FillSequential(_sourceBigArray.AsBigSpan());
        BenchmarkHelpers.FillSequential(_destinationBigArray.AsBigSpan());
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Fill")]
    public ushort JaggedFill()
    {
        JaggedArray<ushort> jagged = _destinationJagged;

        for (int i = 0; i < jagged.ChunkCount; i++)
        {
            jagged.GetChunkSpan(i).Fill((ushort)42);
        }

        return jagged[jagged.Length - 1];
    }

    [Benchmark]
    [BenchmarkCategory("Fill")]
    public ushort BigArrayFill()
    {
        _destinationBigArray.AsBigSpan().Fill((ushort)42);
        return _destinationBigArray[(nint)(Length - 1)];
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("CopyTo")]
    public ushort JaggedCopyTo()
    {
        JaggedArray<ushort> source = _sourceJagged;
        JaggedArray<ushort> destination = _destinationJagged;

        for (int i = 0; i < source.ChunkCount; i++)
        {
            source.GetChunkSpan(i).CopyTo(destination.GetChunkSpan(i));
        }

        return destination[destination.Length - 1];
    }

    [Benchmark]
    [BenchmarkCategory("CopyTo")]
    public ushort BigArrayCopyTo()
    {
        _sourceBigArray.AsBigSpan().CopyTo(_destinationBigArray.AsBigSpan());
        return _destinationBigArray[(nint)(Length - 1)];
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SequenceEqual")]
    public bool JaggedSequenceEqual()
    {
        JaggedArray<ushort> source = _sourceJagged;
        JaggedArray<ushort> destination = _destinationJagged;

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
        ushort target = BenchmarkHelpers.UInt16SearchValue;

        while (low <= high)
        {
            long index = low + ((high - low) >> 1);
            ushort current = _sourceJagged[(nint)index];

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
        return _sourceBigArray.AsBigSpan().BinarySearch(BenchmarkHelpers.UInt16SearchValue);
    }

}
