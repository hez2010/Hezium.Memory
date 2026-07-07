namespace Hezium.Memory.Benchmarks;

internal static class BenchmarkHelpers
{
    internal const int SearchValue = int.MaxValue;

    internal static long[] CreateRandomIndices(long length, int count)
    {
        long[] indices = new long[count];
        Random random = new(42);

        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = random.NextInt64(length);
        }

        return indices;
    }

    internal static void FillSequential(JaggedArray<int> jagged)
    {
        ulong index = 0;
        ulong lastIndex = (ulong)(jagged.Length - 1);

        for (int i = 0; i < jagged.ChunkCount; i++)
        {
            Span<int> chunk = jagged.GetChunkSpan(i);
            for (int j = 0; j < chunk.Length; j++)
            {
                chunk[j] = GetInt32SequentialValue(index++, lastIndex);
            }
        }
    }

    internal static void FillSequential(BigSpan<int> span)
    {
        ulong lastIndex = (ulong)(span.Length - 1);

        for (nint i = 0; i < span.Length; i++)
        {
            span[i] = GetInt32SequentialValue((ulong)i, lastIndex);
        }
    }

    private static int GetInt32SequentialValue(ulong index, ulong lastIndex)
    {
        return lastIndex == 0
            ? 0
            : (int)(index * SearchValue / lastIndex);
    }

    internal static void FillByteSearchData(JaggedArray<byte> jagged, byte terminalValue)
    {
        for (int i = 0; i < jagged.ChunkCount; i++)
        {
            jagged.GetChunkSpan(i).Fill(1);
        }

        jagged[jagged.Length - 1] = terminalValue;
    }

    internal static void FillByteSearchData(BigSpan<byte> span, byte terminalValue)
    {
        span.Fill((byte)1);
        span[span.Length - 1] = terminalValue;
    }
}
