using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hezium.Memory;

/// <summary>
/// Represents a one-dimensional, zero-based collection that can expose more than <see cref="Array.MaxLength"/> logical elements.
/// </summary>
/// <typeparam name="T">The type of elements in the array.</typeparam>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="BigArray{T}"/> class with the specified length.
    /// </summary>
    /// <param name="length">The number of elements in the array.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is negative or greater than <see cref="MaxLength"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigArray(nint length)
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");
        if (length > MaxLength) throw new ArgumentOutOfRangeException(nameof(length), $"Length must be less than or equal to {MaxLength}.");
        if (length <= Array.MaxLength) _storage = new ElementChunk1[length];
        else _storage = CreateBigArraySlow(length);
        _length = length;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BigArray{T}"/> class that wraps the specified array.
    /// </summary>
    /// <param name="array">The array to wrap.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> is null.</exception>
    public BigArray(T[] array)
    {
        if (array is null) throw new ArgumentNullException(nameof(array));
        _length = array.Length;
        _storage = new ElementChunk1[array.Length];
        array.CopyTo(_storage, 0);
    }

    /// <summary>
    /// Gets an empty <see cref="BigArray{T}"/>.
    /// </summary>
    public static BigArray<T> Empty { get; } = new(0);

    /// <summary>
    /// Gets the number of elements in the array.
    /// </summary>
    public nint Length => _length;

    /// <summary>
    /// Gets a value that indicates whether the array is empty.
    /// </summary>
    public bool IsEmpty => _length == 0;

    /// <summary>
    /// Gets a reference to the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <returns>A reference to the element at <paramref name="index"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is outside the bounds of the array.</exception>
    public ref T this[nint index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((nuint)index >= (nuint)_length) ThrowOutOfRangeException(index);
            return ref Unsafe.Add(ref GetDataReference(), index);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    internal static void ThrowOutOfRangeException(nint index)
    {
        throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref T GetDataReference()
    {
        return ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(_storage));
    }

    /// <summary>
    /// Gets the maximum supported logical length for a <see cref="BigArray{T}"/>.
    /// </summary>
    public static nint MaxLength
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            nint chunkSize = 65535 / Unsafe.SizeOf<T>();
            return chunkSize * Array.MaxLength;
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the array.
    /// </summary>
    /// <returns>An enumerator for the array.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Creates a <see cref="BigSpan{T}"/> over the entire array.
    /// </summary>
    /// <returns>A <see cref="BigSpan{T}"/> over the array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigSpan<T> AsBigSpan()
    {
        return new BigSpan<T>(ref GetDataReference(), _length);
    }

    /// <summary>
    /// Creates a <see cref="BigSpan{T}"/> over a range of the array that starts at the specified index.
    /// </summary>
    /// <param name="start">The zero-based index at which the span starts.</param>
    /// <returns>A <see cref="BigSpan{T}"/> over the specified range.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="start"/> is outside the bounds of the array.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigSpan<T> AsBigSpan(nint start)
    {
        return AsBigSpan().Slice(start);
    }

    /// <summary>
    /// Creates a <see cref="BigSpan{T}"/> over a range of the array that starts at the specified index and has the specified length.
    /// </summary>
    /// <param name="start">The zero-based index at which the span starts.</param>
    /// <param name="length">The number of elements in the span.</param>
    /// <returns>A <see cref="BigSpan{T}"/> over the specified range.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the requested range is outside the bounds of the array.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BigSpan<T> AsBigSpan(nint start, nint length)
    {
        return AsBigSpan().Slice(start, length);
    }

    /// <summary>
    /// Creates a <see cref="Span{T}"/> over a range of the array that starts at the specified index and has the specified length.
    /// </summary>
    /// <param name="start">The zero-based index at which the span starts.</param>
    /// <param name="length">The number of elements in the span.</param>
    /// <returns>A <see cref="Span{T}"/> over the specified range.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the requested range is outside the bounds of the array.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan(nint start, int length)
    {
        if ((nuint)start > (nuint)_length || (nuint)(nint)length > (nuint)(_length - start)) ThrowOutOfRangeException(start);
        return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref GetDataReference(), start), length);
    }
}
