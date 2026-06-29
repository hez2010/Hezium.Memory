using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hezium.Collections;

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

    extension<T>(BigSpan<T> span)
    {
        /// <summary>
        /// Copies the elements of the current <see cref="BigSpan{T}"/> to a destination <see cref="BigSpan{T}"/>.
        /// </summary>
        /// <param name="destination">The destination <see cref="BigSpan{T}"/> to copy the elements to.</param>
        public void CopyTo(BigSpan<T> destination)
        {
            if (span._length > destination._length) ThrowArgumentException("Destination span is too small.", nameof(destination));
            if (span._length == 0) return;
            nint remaining = span._length;
            while (remaining > Array.MaxLength)
            {
                MemoryMarshal.CreateSpan(ref span._first, Array.MaxLength).CopyTo(MemoryMarshal.CreateSpan(ref destination._first, Array.MaxLength));
                remaining -= Array.MaxLength;
                span = span.Slice(Array.MaxLength);
                destination = destination.Slice(Array.MaxLength);
            }
            if (remaining > 0)
            {
                MemoryMarshal.CreateSpan(ref span._first, (int)remaining).CopyTo(MemoryMarshal.CreateSpan(ref destination._first, (int)remaining));
            }
        }

        /// <summary>
        /// Determines whether two <see cref="BigSpan{T}"/> instances are equal by comparing their elements.
        /// </summary>
        /// <param name="other">The other <see cref="BigSpan{T}"/> to compare to.</param>
        /// <param name="comparer">An optional equality comparer to use for the comparison.</param>
        /// <returns><c>true</c> if the <see cref="BigSpan{T}"/> instances are equal; otherwise, <c>false</c>.</returns>
        public bool SequenceEqual(BigSpan<T> other, IEqualityComparer<T>? comparer = null)
        {
            if (span._length != other._length) return false;
            if (span._length == 0) return true;
            nint remaining = span._length;
            while (remaining > Array.MaxLength)
            {
                if (!MemoryMarshal.CreateSpan(ref span._first, Array.MaxLength).SequenceEqual(MemoryMarshal.CreateSpan(ref other._first, Array.MaxLength), comparer))
                {
                    return false;
                }
                remaining -= Array.MaxLength;
                span = span.Slice(Array.MaxLength);
                other = other.Slice(Array.MaxLength);
            }
            if (remaining > 0)
            {
                return MemoryMarshal.CreateSpan(ref span._first, (int)remaining).SequenceEqual(MemoryMarshal.CreateSpan(ref other._first, (int)remaining), comparer);
            }
            return true;
        }

        /// <summary>
        /// Compares two <see cref="BigSpan{T}"/> instances lexicographically.
        /// </summary>
        /// <param name="other">The other <see cref="BigSpan{T}"/> to compare to.</param>
        /// <param name="comparer">An optional comparer to use for the comparison.</param>
        /// <returns>A value indicating the relative order of the <see cref="BigSpan{T}"/> instances.</returns>
        public int SequenceCompareTo(BigSpan<T> other, IComparer<T>? comparer = null)
        {
            nint minLength = Math.Min(span._length, other._length);
            nint remaining = minLength;
            while (remaining > Array.MaxLength)
            {
                int result = MemoryMarshal.CreateSpan(ref span._first, Array.MaxLength).SequenceCompareTo(MemoryMarshal.CreateSpan(ref other._first, Array.MaxLength), comparer);
                if (result != 0) return result;
                remaining -= Array.MaxLength;
                span = span.Slice(Array.MaxLength);
                other = other.Slice(Array.MaxLength);
            }
            if (remaining > 0)
            {
                int result = MemoryMarshal.CreateSpan(ref span._first, (int)remaining).SequenceCompareTo(MemoryMarshal.CreateSpan(ref other._first, (int)remaining), comparer);
                if (result != 0) return result;
            }
            return span._length.CompareTo(other._length);
        }

        /// <summary>
        /// Determines whether the <see cref="BigSpan{T}"/> contains a specific value.
        /// </summary>
        /// <param name="value">The value to locate in the <see cref="BigSpan{T}"/>.</param>
        /// <param name="comparer">An optional equality comparer to use for the comparison.</param>
        /// <returns><c>true</c> if the <see cref="BigSpan{T}"/> contains the specified value; otherwise, <c>false</c>.</returns>
        public bool Contains(T value, IEqualityComparer<T>? comparer = null)
        {
            nint remaining = span._length;
            while (remaining > Array.MaxLength)
            {
                if (MemoryMarshal.CreateSpan(ref span._first, Array.MaxLength).Contains(value, comparer))
                {
                    return true;
                }
                remaining -= Array.MaxLength;
                span = span.Slice(Array.MaxLength);
            }
            if (remaining > 0)
            {
                return MemoryMarshal.CreateSpan(ref span._first, (int)remaining).Contains(value, comparer);
            }
            return false;
        }
    }
}
