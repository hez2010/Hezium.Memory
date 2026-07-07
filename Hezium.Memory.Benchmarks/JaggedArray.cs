using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hezium.Memory.Benchmarks;

public sealed class JaggedArray<T>
{
    private const int MaxChunkByteLength = 65535;
    private static readonly int s_chunkLength = CreateChunkLength();
    private readonly T[][] _chunks;

    public JaggedArray(nint length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        Length = length;
        _chunks = new T[GetChunkCount(length)][];

        for (int i = 0; i < _chunks.Length; i++)
        {
            _chunks[i] = new T[GetChunkLength(length, i)];
        }
    }

    public nint Length { get; }

    public int ChunkCount => _chunks.Length;

    public Span<T> GetChunkSpan(int chunkIndex)
    {
        return _chunks[chunkIndex];
    }

    public ref T this[nint index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (index < 0 || (nuint)index >= (nuint)Length)
            {
                ThrowOutOfRange(nameof(index));
            }

            ref var chunk = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_chunks), (int)(index / s_chunkLength));
            return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(chunk), (int)(index % s_chunkLength));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowOutOfRange(string paramName)
    {
        throw new ArgumentOutOfRangeException(paramName);
    }

    private static int CreateChunkLength()
    {
        int elementSize = Unsafe.SizeOf<T>();
        return elementSize > MaxChunkByteLength
            ? throw new NotSupportedException($"Type {typeof(T)} is too large to be used with {nameof(JaggedArray<>)}.")
            : MaxChunkByteLength / elementSize;
    }

    private static int GetChunkCount(nint length)
    {
        nint chunks = (length / s_chunkLength) + (length % s_chunkLength == 0 ? 0 : 1);
        return checked((int)chunks);
    }

    private static int GetChunkLength(nint length, int chunkIndex)
    {
        nint start = (nint)chunkIndex * s_chunkLength;
        nint remaining = length - start;
        return remaining > s_chunkLength ? s_chunkLength : (int)remaining;
    }
}
