using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hezium.Memory.Benchmarks;

public sealed class JaggedArray<T>
{
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            ref var chunk = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_chunks), (int)(index / Array.MaxLength));
            return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(chunk), (int)(index % Array.MaxLength));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowOutOfRange(string paramName)
    {
        throw new ArgumentOutOfRangeException(paramName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetChunkCount(nint length)
    {
        nint chunks = (length / Array.MaxLength) + (length % Array.MaxLength == 0 ? 0 : 1);
        return checked((int)chunks);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetChunkLength(nint length, int chunkIndex)
    {
        nint start = (nint)chunkIndex * Array.MaxLength;
        nint remaining = length - start;
        return remaining > Array.MaxLength ? Array.MaxLength : (int)remaining;
    }
}
