using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hezium.Collections;

/// <summary>
/// Provides extension methods for <see cref="BigArray{T}"/>, <see cref="BigSpan{T}"/>, <see cref="BigReadOnlySpan{T}"/>, and <see cref="MemoryMarshal"/>.
/// </summary>
public static class BigArrayExtensions
{
    extension(MemoryMarshal)
    {
        /// <summary>
        /// Creates a <see cref="BigSpan{T}"/> from a reference to the first element and a specified length.
        /// </summary>
        /// <typeparam name="T">The type of elements in the <see cref="BigSpan{T}"/>.</typeparam>
        /// <param name="first">A reference to the first element of the span.</param>
        /// <param name="length">The number of elements in the span.</param>
        /// <returns>A <see cref="BigSpan{T}"/> that represents the specified range of elements.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is negative.</exception>
        public static BigSpan<T> CreateBigSpan<T>(ref T first, nint length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");
            return new BigSpan<T>(ref first, length);
        }

        /// <summary>
        /// Gets a reference to the first element of a <see cref="BigSpan{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the <see cref="BigSpan{T}"/>.</typeparam>
        /// <param name="span">The <see cref="BigSpan{T}"/> to get the reference from.</param>
        /// <returns>A reference to the first element of the <see cref="BigSpan{T}"/>.</returns>
        public static ref T GetReference<T>(BigSpan<T> span)
        {
            return ref span._first;
        }

        /// <summary>
        /// Gets a read-only reference to the first element of a <see cref="BigReadOnlySpan{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the <see cref="BigReadOnlySpan{T}"/>.</typeparam>
        /// <param name="span">The <see cref="BigReadOnlySpan{T}"/> to get the reference from.</param>
        /// <returns>A read-only reference to the first element of the <see cref="BigReadOnlySpan{T}"/>.</returns>
        public static ref readonly T GetReference<T>(BigReadOnlySpan<T> span)
        {
            return ref span._first;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowArgumentException(string message, string paramName)
    {
        throw new ArgumentException(message, paramName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BigReadOnlySpan<T> AsReadOnlySpan<T>(BigSpan<T> span)
    {
        return new BigReadOnlySpan<T>(ref span._first, span._length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Span<T> CreateSpan<T>(BigSpan<T> span, int length)
    {
        return MemoryMarshal.CreateSpan(ref span._first, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<T> CreateReadOnlySpan<T>(BigReadOnlySpan<T> span, int length)
    {
        return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in span._first), length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetChunkLength(nint remaining)
    {
        return remaining > Array.MaxLength ? Array.MaxLength : (int)remaining;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BigSpan<T> SliceUnchecked<T>(BigSpan<T> span, nint start, nint length)
    {
        return new BigSpan<T>(ref Unsafe.Add(ref span._first, start), length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BigReadOnlySpan<T> SliceUnchecked<T>(BigReadOnlySpan<T> span, nint start, nint length)
    {
        return new BigReadOnlySpan<T>(ref Unsafe.Add(ref Unsafe.AsRef(in span._first), start), length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BigSpan<T> SliceUnchecked<T>(BigSpan<T> span, nint start)
    {
        return SliceUnchecked(span, start, span._length - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BigReadOnlySpan<T> SliceUnchecked<T>(BigReadOnlySpan<T> span, nint start)
    {
        return SliceUnchecked(span, start, span._length - start);
    }

    private static bool Overlaps<T>(BigReadOnlySpan<T> source, BigSpan<T> destination, out nint elementOffset)
    {
        ref T sourceReference = ref Unsafe.AsRef(in source._first);
        nint byteOffset = Unsafe.ByteOffset(ref sourceReference, ref destination._first);
        nint elementSize = Unsafe.SizeOf<T>();
        if (byteOffset % elementSize != 0)
        {
            elementOffset = 0;
            return false;
        }

        elementOffset = byteOffset / elementSize;
        return elementOffset < source._length && elementOffset > -destination._length;
    }

    private static void CopyToCore<T>(BigReadOnlySpan<T> source, BigSpan<T> destination)
    {
        if (source._length == 0) return;

        if (Overlaps(source, destination, out nint elementOffset) && elementOffset > 0)
        {
            nint remaining = source._length;
            while (remaining > 0)
            {
                int chunkLength = GetChunkLength(remaining);
                nint chunkStart = remaining - chunkLength;
                CreateReadOnlySpan(SliceUnchecked(source, chunkStart, chunkLength), chunkLength).CopyTo(CreateSpan(SliceUnchecked(destination, chunkStart, chunkLength), chunkLength));
                remaining = chunkStart;
            }

            return;
        }

        while (source._length > 0)
        {
            int chunkLength = GetChunkLength(source._length);
            CreateReadOnlySpan(source, chunkLength).CopyTo(CreateSpan(destination, chunkLength));
            source = SliceUnchecked(source, chunkLength);
            destination = SliceUnchecked(destination, chunkLength);
        }
    }

    private static void CopyToCore<T>(BigReadOnlySpan<T> source, Span<T> destination)
    {
        if (source._length == 0) return;
        CreateReadOnlySpan(source, (int)source._length).CopyTo(destination);
    }

    private static T[] ToArrayCore<T>(BigReadOnlySpan<T> span)
    {
        if (span._length > Array.MaxLength)
        {
            throw new InvalidOperationException("The span is too large to copy into a single array.");
        }

        T[] result = new T[(int)span._length];
        CopyToCore(span, result);
        return result;
    }

    private static BigArray<T> ToBigArrayCore<T>(BigReadOnlySpan<T> span)
    {
        BigArray<T> result = new(span._length);
        CopyToCore(span, result.AsBigSpan());
        return result;
    }

    private static bool SequenceEqualCore<T>(BigReadOnlySpan<T> span, BigReadOnlySpan<T> other, IEqualityComparer<T>? comparer)
    {
        if (span._length != other._length) return false;

        while (span._length > 0)
        {
            int chunkLength = GetChunkLength(span._length);
            if (!CreateReadOnlySpan(span, chunkLength).SequenceEqual(CreateReadOnlySpan(other, chunkLength), comparer))
            {
                return false;
            }

            span = SliceUnchecked(span, chunkLength);
            other = SliceUnchecked(other, chunkLength);
        }

        return true;
    }

    private static int SequenceCompareToCore<T>(BigReadOnlySpan<T> span, BigReadOnlySpan<T> other, IComparer<T>? comparer)
    {
        nint remaining = Math.Min(span._length, other._length);
        while (remaining > 0)
        {
            int chunkLength = GetChunkLength(remaining);
            int result = CreateReadOnlySpan(span, chunkLength).SequenceCompareTo(CreateReadOnlySpan(other, chunkLength), comparer);
            if (result != 0) return result;

            remaining -= chunkLength;
            span = SliceUnchecked(span, chunkLength);
            other = SliceUnchecked(other, chunkLength);
        }

        return span._length.CompareTo(other._length);
    }

    private static nint IndexOfCore<T>(BigReadOnlySpan<T> span, T value, IEqualityComparer<T>? comparer)
    {
        nint offset = 0;
        while (span._length > 0)
        {
            int chunkLength = GetChunkLength(span._length);
            int index = CreateReadOnlySpan(span, chunkLength).IndexOf(value, comparer);
            if (index >= 0) return offset + index;

            offset += chunkLength;
            span = SliceUnchecked(span, chunkLength);
        }

        return -1;
    }

    private static nint LastIndexOfCore<T>(BigReadOnlySpan<T> span, T value, IEqualityComparer<T>? comparer)
    {
        nint remaining = span._length;
        while (remaining > 0)
        {
            int chunkLength = GetChunkLength(remaining);
            nint chunkStart = remaining - chunkLength;
            int index = CreateReadOnlySpan(SliceUnchecked(span, chunkStart, chunkLength), chunkLength).LastIndexOf(value, comparer);
            if (index >= 0) return chunkStart + index;

            remaining = chunkStart;
        }

        return -1;
    }

    private static bool StartsWithCore<T>(BigReadOnlySpan<T> span, BigReadOnlySpan<T> value, IEqualityComparer<T>? comparer)
    {
        return value._length <= span._length && SequenceEqualCore(SliceUnchecked(span, 0, value._length), value, comparer);
    }

    private static bool EndsWithCore<T>(BigReadOnlySpan<T> span, BigReadOnlySpan<T> value, IEqualityComparer<T>? comparer)
    {
        return value._length <= span._length && SequenceEqualCore(SliceUnchecked(span, span._length - value._length, value._length), value, comparer);
    }

    extension<T>(BigArray<T> array)
    {
        /// <summary>
        /// Sets all elements in the array to the default value of <typeparamref name="T"/>.
        /// </summary>
        public void Clear()
        {
            array.AsBigSpan().Clear();
        }

        /// <summary>
        /// Fills the array with the specified value.
        /// </summary>
        /// <param name="value">The value to assign to each element.</param>
        public void Fill(T value)
        {
            array.AsBigSpan().Fill(value);
        }

        /// <summary>
        /// Copies the array to a destination <see cref="BigArray{T}"/>.
        /// </summary>
        /// <param name="destination">The destination array.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="destination"/> is too small.</exception>
        public void CopyTo(BigArray<T> destination)
        {
            array.AsBigSpan().CopyTo(destination.AsBigSpan());
        }

        /// <summary>
        /// Copies the array to a destination <see cref="BigArray{T}"/> starting at the specified destination index.
        /// </summary>
        /// <param name="destination">The destination array.</param>
        /// <param name="destinationIndex">The zero-based destination index at which copying begins.</param>
        /// <exception cref="ArgumentException">Thrown when the destination range is too small.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="destinationIndex"/> is outside the bounds of <paramref name="destination"/>.</exception>
        public void CopyTo(BigArray<T> destination, nint destinationIndex)
        {
            array.AsBigSpan().CopyTo(destination.AsBigSpan(destinationIndex));
        }

        /// <summary>
        /// Attempts to copy the array to a destination <see cref="BigArray{T}"/>.
        /// </summary>
        /// <param name="destination">The destination array.</param>
        /// <returns><see langword="true"/> if the copy succeeded; otherwise, <see langword="false"/>.</returns>
        public bool TryCopyTo(BigArray<T> destination)
        {
            return array.AsBigSpan().TryCopyTo(destination.AsBigSpan());
        }

        /// <summary>
        /// Copies the array to a new single-dimensional array.
        /// </summary>
        /// <returns>A new array containing the copied elements.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the array is too large to fit in a single managed array.</exception>
        public T[] ToArray()
        {
            return array.AsBigSpan().ToArray();
        }

        /// <summary>
        /// Searches for the specified value and returns the index of its first occurrence.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <param name="comparer">The comparer to use, or <see langword="null"/> to use the default equality comparer.</param>
        /// <returns>The index of the first occurrence of <paramref name="value"/>, or -1 if it is not found.</returns>
        public nint IndexOf(T value, IEqualityComparer<T>? comparer = null)
        {
            return array.AsBigSpan().IndexOf(value, comparer);
        }

        /// <summary>
        /// Searches for the specified value and returns the index of its last occurrence.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <param name="comparer">The comparer to use, or <see langword="null"/> to use the default equality comparer.</param>
        /// <returns>The index of the last occurrence of <paramref name="value"/>, or -1 if it is not found.</returns>
        public nint LastIndexOf(T value, IEqualityComparer<T>? comparer = null)
        {
            return array.AsBigSpan().LastIndexOf(value, comparer);
        }

        /// <summary>
        /// Determines whether the array contains the specified value.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <param name="comparer">The comparer to use, or <see langword="null"/> to use the default equality comparer.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is found; otherwise, <see langword="false"/>.</returns>
        public bool Contains(T value, IEqualityComparer<T>? comparer = null)
        {
            return array.AsBigSpan().Contains(value, comparer);
        }
    }

    extension<T>(BigSpan<T> span)
    {
        /// <summary>
        /// Sets all elements in the span to the default value of <typeparamref name="T"/>.
        /// </summary>
        public void Clear()
        {
            nint remaining = span._length;
            while (remaining > 0)
            {
                int chunkLength = GetChunkLength(remaining);
                CreateSpan(span, chunkLength).Clear();
                remaining -= chunkLength;
                span = SliceUnchecked(span, chunkLength);
            }
        }

        /// <summary>
        /// Fills the span with the specified value.
        /// </summary>
        /// <param name="value">The value to assign to each element.</param>
        public void Fill(T value)
        {
            nint remaining = span._length;
            while (remaining > 0)
            {
                int chunkLength = GetChunkLength(remaining);
                CreateSpan(span, chunkLength).Fill(value);
                remaining -= chunkLength;
                span = SliceUnchecked(span, chunkLength);
            }
        }

        /// <summary>
        /// Copies the elements of the current span to a destination <see cref="BigSpan{T}"/>.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="destination"/> is too small.</exception>
        public void CopyTo(BigSpan<T> destination)
        {
            if (span._length > destination._length) ThrowArgumentException("Destination span is too small.", nameof(destination));
            CopyToCore(AsReadOnlySpan(span), destination);
        }

        /// <summary>
        /// Copies the elements of the current span to a destination <see cref="Span{T}"/>.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="destination"/> is too small.</exception>
        public void CopyTo(Span<T> destination)
        {
            if (span._length > destination.Length) ThrowArgumentException("Destination span is too small.", nameof(destination));
            CopyToCore(AsReadOnlySpan(span), destination);
        }

        /// <summary>
        /// Attempts to copy the current span to a destination <see cref="BigSpan{T}"/>.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        /// <returns><see langword="true"/> if the copy succeeded; otherwise, <see langword="false"/>.</returns>
        public bool TryCopyTo(BigSpan<T> destination)
        {
            if (span._length > destination._length) return false;
            CopyToCore(AsReadOnlySpan(span), destination);
            return true;
        }

        /// <summary>
        /// Attempts to copy the current span to a destination <see cref="Span{T}"/>.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        /// <returns><see langword="true"/> if the copy succeeded; otherwise, <see langword="false"/>.</returns>
        public bool TryCopyTo(Span<T> destination)
        {
            if (span._length > destination.Length) return false;
            CopyToCore(AsReadOnlySpan(span), destination);
            return true;
        }

        /// <summary>
        /// Copies the contents of the span into a new single-dimensional array.
        /// </summary>
        /// <returns>A new array containing the copied elements.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the span is too large to fit in a single managed array.</exception>
        public T[] ToArray()
        {
            return ToArrayCore(AsReadOnlySpan(span));
        }

        /// <summary>
        /// Copies the contents of the span into a new <see cref="BigArray{T}"/>.
        /// </summary>
        /// <returns>A new <see cref="BigArray{T}"/> containing the copied elements.</returns>
        public BigArray<T> ToBigArray()
        {
            return ToBigArrayCore(AsReadOnlySpan(span));
        }

        /// <summary>
        /// Creates a <see cref="Span{T}"/> over a range of the current span.
        /// </summary>
        /// <param name="start">The zero-based index at which the span starts.</param>
        /// <param name="length">The number of elements in the span.</param>
        /// <returns>A <see cref="Span{T}"/> over the specified range.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the requested range is outside the bounds of the span.</exception>
        public Span<T> ToSpan(nint start, int length)
        {
            if ((nuint)start > (nuint)span._length || (nuint)(nint)length > (nuint)(span._length - start)) BigArray<T>.ThrowOutOfRangeException(start);
            return CreateSpan(SliceUnchecked(span, start, length), length);
        }

        /// <summary>
        /// Searches for the specified value and returns the index of its first occurrence.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <param name="comparer">The comparer to use, or <see langword="null"/> to use the default equality comparer.</param>
        /// <returns>The index of the first occurrence of <paramref name="value"/>, or -1 if it is not found.</returns>
        public nint IndexOf(T value, IEqualityComparer<T>? comparer = null)
        {
            return IndexOfCore(AsReadOnlySpan(span), value, comparer);
        }

        /// <summary>
        /// Searches for the specified value and returns the index of its last occurrence.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <param name="comparer">The comparer to use, or <see langword="null"/> to use the default equality comparer.</param>
        /// <returns>The index of the last occurrence of <paramref name="value"/>, or -1 if it is not found.</returns>
        public nint LastIndexOf(T value, IEqualityComparer<T>? comparer = null)
        {
            return LastIndexOfCore(AsReadOnlySpan(span), value, comparer);
        }

        /// <summary>
        /// Determines whether the span contains the specified value.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <param name="comparer">The comparer to use, or <see langword="null"/> to use the default equality comparer.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is found; otherwise, <see langword="false"/>.</returns>
        public bool Contains(T value, IEqualityComparer<T>? comparer = null)
        {
            return IndexOfCore(AsReadOnlySpan(span), value, comparer) >= 0;
        }

        /// <summary>
        /// Determines whether the current span and another span contain the same elements.
        /// </summary>
        /// <param name="other">The span to compare with the current span.</param>
        /// <param name="comparer">The comparer to use, or <see langword="null"/> to use the default equality comparer.</param>
        /// <returns><see langword="true"/> if the spans contain the same elements; otherwise, <see langword="false"/>.</returns>
        public bool SequenceEqual(BigSpan<T> other, IEqualityComparer<T>? comparer = null)
        {
            return SequenceEqualCore(AsReadOnlySpan(span), AsReadOnlySpan(other), comparer);
        }

        /// <summary>
        /// Determines whether the current span and another read-only span contain the same elements.
        /// </summary>
        /// <param name="other">The span to compare with the current span.</param>
        /// <param name="comparer">The comparer to use, or <see langword="null"/> to use the default equality comparer.</param>
        /// <returns><see langword="true"/> if the spans contain the same elements; otherwise, <see langword="false"/>.</returns>
        public bool SequenceEqual(BigReadOnlySpan<T> other, IEqualityComparer<T>? comparer = null)
        {
            return SequenceEqualCore(AsReadOnlySpan(span), other, comparer);
        }

        /// <summary>
        /// Compares the current span with another span lexicographically.
        /// </summary>
        /// <param name="other">The span to compare with the current span.</param>
        /// <param name="comparer">The comparer to use, or <see langword="null"/> to use the default comparer.</param>
        /// <returns>A value that indicates the relative order of the spans.</returns>
        public int SequenceCompareTo(BigSpan<T> other, IComparer<T>? comparer = null)
        {
            return SequenceCompareToCore(AsReadOnlySpan(span), AsReadOnlySpan(other), comparer);
        }

        /// <summary>
        /// Compares the current span with another read-only span lexicographically.
        /// </summary>
        /// <param name="other">The span to compare with the current span.</param>
        /// <param name="comparer">The comparer to use, or <see langword="null"/> to use the default comparer.</param>
        /// <returns>A value that indicates the relative order of the spans.</returns>
        public int SequenceCompareTo(BigReadOnlySpan<T> other, IComparer<T>? comparer = null)
        {
            return SequenceCompareToCore(AsReadOnlySpan(span), other, comparer);
        }

        /// <summary>
        /// Determines whether the beginning of the current span matches another span.
        /// </summary>
        /// <param name="value">The span to compare with the start of the current span.</param>
        /// <param name="comparer">The comparer to use, or <see langword="null"/> to use the default equality comparer.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> matches the start of the current span; otherwise, <see langword="false"/>.</returns>
        public bool StartsWith(BigReadOnlySpan<T> value, IEqualityComparer<T>? comparer = null)
        {
            return StartsWithCore(AsReadOnlySpan(span), value, comparer);
        }

        /// <summary>
        /// Determines whether the end of the current span matches another span.
        /// </summary>
        /// <param name="value">The span to compare with the end of the current span.</param>
        /// <param name="comparer">The comparer to use, or <see langword="null"/> to use the default equality comparer.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> matches the end of the current span; otherwise, <see langword="false"/>.</returns>
        public bool EndsWith(BigReadOnlySpan<T> value, IEqualityComparer<T>? comparer = null)
        {
            return EndsWithCore(AsReadOnlySpan(span), value, comparer);
        }
    }

    extension<T>(BigReadOnlySpan<T> span)
    {
        /// <summary>
        /// Copies the elements of the current read-only span to a destination <see cref="BigSpan{T}"/>.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="destination"/> is too small.</exception>
        public void CopyTo(BigSpan<T> destination)
        {
            if (span._length > destination._length) ThrowArgumentException("Destination span is too small.", nameof(destination));
            CopyToCore(span, destination);
        }

        /// <summary>
        /// Copies the elements of the current read-only span to a destination <see cref="Span{T}"/>.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="destination"/> is too small.</exception>
        public void CopyTo(Span<T> destination)
        {
            if (span._length > destination.Length) ThrowArgumentException("Destination span is too small.", nameof(destination));
            CopyToCore(span, destination);
        }

        /// <summary>
        /// Attempts to copy the current read-only span to a destination <see cref="BigSpan{T}"/>.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        /// <returns><see langword="true"/> if the copy succeeded; otherwise, <see langword="false"/>.</returns>
        public bool TryCopyTo(BigSpan<T> destination)
        {
            if (span._length > destination._length) return false;
            CopyToCore(span, destination);
            return true;
        }

        /// <summary>
        /// Attempts to copy the current read-only span to a destination <see cref="Span{T}"/>.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        /// <returns><see langword="true"/> if the copy succeeded; otherwise, <see langword="false"/>.</returns>
        public bool TryCopyTo(Span<T> destination)
        {
            if (span._length > destination.Length) return false;
            CopyToCore(span, destination);
            return true;
        }

        /// <summary>
        /// Copies the contents of the read-only span into a new single-dimensional array.
        /// </summary>
        /// <returns>A new array containing the copied elements.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the span is too large to fit in a single managed array.</exception>
        public T[] ToArray()
        {
            return ToArrayCore(span);
        }

        /// <summary>
        /// Copies the contents of the read-only span into a new <see cref="BigArray{T}"/>.
        /// </summary>
        /// <returns>A new <see cref="BigArray{T}"/> containing the copied elements.</returns>
        public BigArray<T> ToBigArray()
        {
            return ToBigArrayCore(span);
        }

        /// <summary>
        /// Creates a <see cref="ReadOnlySpan{T}"/> over a range of the current read-only span.
        /// </summary>
        /// <param name="start">The zero-based index at which the span starts.</param>
        /// <param name="length">The number of elements in the span.</param>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> over the specified range.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the requested range is outside the bounds of the span.</exception>
        public ReadOnlySpan<T> ToSpan(nint start, int length)
        {
            if ((nuint)start > (nuint)span._length || (nuint)(nint)length > (nuint)(span._length - start)) BigArray<T>.ThrowOutOfRangeException(start);
            return CreateReadOnlySpan(SliceUnchecked(span, start, length), length);
        }

        /// <summary>
        /// Searches for the specified value and returns the index of its first occurrence.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <param name="comparer">The comparer to use, or <see langword="null"/> to use the default equality comparer.</param>
        /// <returns>The index of the first occurrence of <paramref name="value"/>, or -1 if it is not found.</returns>
        public nint IndexOf(T value, IEqualityComparer<T>? comparer = null)
        {
            return IndexOfCore(span, value, comparer);
        }

        /// <summary>
        /// Searches for the specified value and returns the index of its last occurrence.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <param name="comparer">The comparer to use, or <see langword="null"/> to use the default equality comparer.</param>
        /// <returns>The index of the last occurrence of <paramref name="value"/>, or -1 if it is not found.</returns>
        public nint LastIndexOf(T value, IEqualityComparer<T>? comparer = null)
        {
            return LastIndexOfCore(span, value, comparer);
        }

        /// <summary>
        /// Determines whether the read-only span contains the specified value.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <param name="comparer">The comparer to use, or <see langword="null"/> to use the default equality comparer.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is found; otherwise, <see langword="false"/>.</returns>
        public bool Contains(T value, IEqualityComparer<T>? comparer = null)
        {
            return IndexOfCore(span, value, comparer) >= 0;
        }

        /// <summary>
        /// Determines whether the current read-only span and another read-only span contain the same elements.
        /// </summary>
        /// <param name="other">The span to compare with the current span.</param>
        /// <param name="comparer">The comparer to use, or <see langword="null"/> to use the default equality comparer.</param>
        /// <returns><see langword="true"/> if the spans contain the same elements; otherwise, <see langword="false"/>.</returns>
        public bool SequenceEqual(BigReadOnlySpan<T> other, IEqualityComparer<T>? comparer = null)
        {
            return SequenceEqualCore(span, other, comparer);
        }

        /// <summary>
        /// Compares the current read-only span with another read-only span lexicographically.
        /// </summary>
        /// <param name="other">The span to compare with the current span.</param>
        /// <param name="comparer">The comparer to use, or <see langword="null"/> to use the default comparer.</param>
        /// <returns>A value that indicates the relative order of the spans.</returns>
        public int SequenceCompareTo(BigReadOnlySpan<T> other, IComparer<T>? comparer = null)
        {
            return SequenceCompareToCore(span, other, comparer);
        }

        /// <summary>
        /// Determines whether the beginning of the current read-only span matches another span.
        /// </summary>
        /// <param name="value">The span to compare with the start of the current span.</param>
        /// <param name="comparer">The comparer to use, or <see langword="null"/> to use the default equality comparer.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> matches the start of the current span; otherwise, <see langword="false"/>.</returns>
        public bool StartsWith(BigReadOnlySpan<T> value, IEqualityComparer<T>? comparer = null)
        {
            return StartsWithCore(span, value, comparer);
        }

        /// <summary>
        /// Determines whether the end of the current read-only span matches another span.
        /// </summary>
        /// <param name="value">The span to compare with the end of the current span.</param>
        /// <param name="comparer">The comparer to use, or <see langword="null"/> to use the default equality comparer.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> matches the end of the current span; otherwise, <see langword="false"/>.</returns>
        public bool EndsWith(BigReadOnlySpan<T> value, IEqualityComparer<T>? comparer = null)
        {
            return EndsWithCore(span, value, comparer);
        }
    }
}
