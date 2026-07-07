using System.Buffers;
using BenchmarkDotNet.Attributes;
using Hezium.Memory;

namespace Hezium.Memory.Benchmarks;

public class SearchValuesBenchmarks
{
    private JaggedArray<byte> _sourceJagged = null!;
    private BigArray<byte> _sourceBigArray = null!;
    private SearchValues<byte> _searchValues = null!;

    [Params(1_048_576L, 4_294_967_296L)]
    public long Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _searchValues = SearchValues.Create([251, 252]);

        _sourceJagged = new JaggedArray<byte>((nint)Length);
        _sourceBigArray = new BigArray<byte>((nint)Length);

        BenchmarkHelpers.FillByteSearchData(_sourceJagged, 251);
        BenchmarkHelpers.FillByteSearchData(_sourceBigArray.AsBigSpan(), 251);
    }

    [Benchmark]
    [BenchmarkCategory("IndexOfAny")]
    public long JaggedIndexOfAny()
    {
        long offset = 0;
        JaggedArray<byte> jagged = _sourceJagged;

        for (int i = 0; i < jagged.ChunkCount; i++)
        {
            Span<byte> chunk = jagged.GetChunkSpan(i);
            int index = chunk.IndexOfAny(_searchValues);
            if (index >= 0)
            {
                return offset + index;
            }

            offset += chunk.Length;
        }

        return -1;
    }

    [Benchmark]
    [BenchmarkCategory("IndexOfAny")]
    public nint BigArrayIndexOfAny()
    {
        return _sourceBigArray.AsBigSpan().IndexOfAny(_searchValues);
    }

    [Benchmark]
    [BenchmarkCategory("LastIndexOfAny")]
    public long JaggedLastIndexOfAny()
    {
        long offset = Length;
        JaggedArray<byte> jagged = _sourceJagged;

        for (int i = jagged.ChunkCount - 1; i >= 0; i--)
        {
            Span<byte> chunk = jagged.GetChunkSpan(i);
            offset -= chunk.Length;
            int index = chunk.LastIndexOfAny(_searchValues);
            if (index >= 0)
            {
                return offset + index;
            }
        }

        return -1;
    }

    [Benchmark]
    [BenchmarkCategory("LastIndexOfAny")]
    public nint BigArrayLastIndexOfAny()
    {
        return _sourceBigArray.AsBigSpan().LastIndexOfAny(_searchValues);
    }
}
