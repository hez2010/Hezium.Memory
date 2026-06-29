using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hezium.Collections;

public sealed partial class BigArray<T> : IEnumerable<T>
{
    internal readonly Array _storage;
    internal readonly nint _length;

    static BigArray()
    {
        if (Unsafe.SizeOf<T>() > 65535)
        {
            throw new NotSupportedException($"Type {typeof(T)} is too large to be used with BigArray.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigArray(nint length)
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");
        if (length > MaxLength) throw new ArgumentOutOfRangeException(nameof(length), $"Length must be less than or equal to {MaxLength}.");
        if (length <= Array.MaxLength) _storage = new ElementChunk1[length];
        else _storage = CreateBigArraySlow(length);
        _length = length;
    }

    public nint Length => _length;

    public ref T this[nint index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (index < 0 || index >= _length) ThrowOutOfRangeException(index);
            return ref Unsafe.Add(ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(_storage)), index);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    internal static void ThrowOutOfRangeException(nint index)
    {
        throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range.");
    }

    public static nint MaxLength
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            nint chunkSize = 65535 / Unsafe.SizeOf<T>();
            return chunkSize * Array.MaxLength;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public BigSpan<T> AsBigSpan()
    {
        return new BigSpan<T>(ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(_storage)), _length);
    }
}
