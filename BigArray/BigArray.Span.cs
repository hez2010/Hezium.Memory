using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hezium.Collections;

/// <summary>
/// Provides a type-safe view over a contiguous logical region of memory that can contain more than <see cref="Array.MaxLength"/> elements.
/// </summary>
/// <typeparam name="T">The type of elements in the span.</typeparam>
public readonly ref struct BigSpan<T>
{
    internal readonly ref T _first;
    internal readonly nint _length;

    /// <summary>
    /// Gets an empty <see cref="BigSpan{T}"/>.
    /// </summary>
    public static BigSpan<T> Empty => default;

    /// <summary>
    /// Gets the number of elements in the span.
    /// </summary>
    public nint Length => _length;

    /// <summary>
    /// Gets a value that indicates whether the span is empty.
    /// </summary>
    public bool IsEmpty => _length == 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="BigSpan{T}"/> struct that represents a single element.
    /// </summary>
    /// <param name="first">A reference to the first element in the span.</param>
    public BigSpan(ref T first)
    {
        _first = ref first;
        _length = 1;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BigSpan{T}"/> struct that represents the entire <see cref="BigArray{T}"/>.
    /// </summary>
    /// <param name="array">The <see cref="BigArray{T}"/> to represent.</param>
    public BigSpan(BigArray<T> array)
    {
        _first = ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array._storage));
        _length = array._length;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BigSpan{T}"/> struct that represents a span of memory starting at the specified pointer and with the specified length.
    /// </summary>
    /// <param name="pointer">A pointer to the first element in the span.</param>
    /// <param name="length">The number of elements in the span.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the length is negative.</exception>
#pragma warning disable CS8500
    public unsafe BigSpan(T* pointer, nint length)
#pragma warning restore CS8500
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");
        _first = ref Unsafe.AsRef<T>(pointer);
        _length = length;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BigSpan{T}"/> struct that represents a span of memory starting at the specified reference and with the specified length.
    /// </summary>
    /// <param name="first">A reference to the first element in the span.</param>
    /// <param name="length">The number of elements in the span.</param>
    internal BigSpan(ref T first, nint length)
    {
        _first = ref first;
        _length = length;
    }

    /// <summary>
    /// Creates a new <see cref="BigSpan{T}"/> that represents a slice of the current span starting at the specified index.
    /// </summary>
    /// <param name="start">The index at which to start the slice.</param>
    /// <returns>A new <see cref="BigSpan{T}"/> that represents the specified slice of the current span.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BigSpan<T> Slice(nint start)
    {
        if ((nuint)start > (nuint)_length) BigArray<T>.ThrowOutOfRangeException(start);
        return new BigSpan<T>(ref Unsafe.Add(ref _first, start), _length - start);
    }

    /// <summary>
    /// Creates a new <see cref="BigSpan{T}"/> that represents a slice of the current span starting at the specified index and with the specified length.
    /// </summary>
    /// <param name="start">The index at which to start the slice.</param>
    /// <param name="length">The number of elements in the slice.</param>
    /// <returns>A new <see cref="BigSpan{T}"/> that represents the specified slice of the current span.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BigSpan<T> Slice(nint start, nint length)
    {
        if ((nuint)start > (nuint)_length || (nuint)length > (nuint)(_length - start)) BigArray<T>.ThrowOutOfRangeException(start);
        return new BigSpan<T>(ref Unsafe.Add(ref _first, start), length);
    }

    /// <summary>
    /// Gets a reference to the element at the specified index in the span.
    /// </summary>
    /// <param name="index">The index of the element to get.</param>
    /// <returns>A reference to the element at the specified index.</returns>
    public readonly ref T this[nint index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((nuint)index >= (nuint)_length) BigArray<T>.ThrowOutOfRangeException(index);
            return ref Unsafe.Add(ref _first, index);
        }
    }

    /// <summary>
    /// Gets a reference to the element that can be used for pinning.
    /// </summary>
    /// <returns>A reference to the first element of the span.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetPinnableReference()
    {
        return ref _first;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="BigSpan{T}"/>.
    /// </summary>
    /// <returns>An enumerator for the <see cref="BigSpan{T}"/>.</returns>
    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    /// <summary>
    /// Defines an implicit conversion from <see cref="BigSpan{T}"/> to <see cref="BigReadOnlySpan{T}"/>.
    /// </summary>
    /// <param name="span">The <see cref="BigSpan{T}"/> to convert.</param>
    public static implicit operator BigReadOnlySpan<T>(BigSpan<T> span)
    {
        return new BigReadOnlySpan<T>(ref span._first, span._length);
    }

    /// <summary>
    /// Enumerates the elements of a <see cref="BigSpan{T}"/>.
    /// </summary>
    public ref struct Enumerator : IEnumerator<T>
    {
        private readonly BigSpan<T> _span;
        private nint _offset;

        internal Enumerator(BigSpan<T> span)
        {
            _span = span;
            _offset = -1;
        }

        /// <summary>
        /// Gets a reference to the current element in the enumerator.
        /// </summary>
        public readonly ref T Current => ref _span[_offset];
        readonly T IEnumerator<T>.Current => Current;
        readonly object? IEnumerator.Current => Current;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (_offset < _span._length - 1)
            {
                _offset++;
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public readonly void Dispose() { }

        /// <inheritdoc/>
        public void Reset()
        {
            _offset = -1;
        }
    }
}

/// <summary>
/// Provides a type-safe read-only view over a contiguous logical region of memory that can contain more than <see cref="Array.MaxLength"/> elements.
/// </summary>
/// <typeparam name="T">The type of elements in the span.</typeparam>
public readonly ref struct BigReadOnlySpan<T>
{
    internal readonly ref readonly T _first;
    internal readonly nint _length;

    /// <summary>
    /// Gets an empty <see cref="BigReadOnlySpan{T}"/>.
    /// </summary>
    public static BigReadOnlySpan<T> Empty => default;

    /// <summary>
    /// Gets the number of elements in the span.
    /// </summary>
    public nint Length => _length;

    /// <summary>
    /// Gets a value that indicates whether the span is empty.
    /// </summary>
    public bool IsEmpty => _length == 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="BigReadOnlySpan{T}"/> struct that represents a single element.
    /// </summary>
    /// <param name="first">A reference to the first element in the span.</param>
    public BigReadOnlySpan(ref T first)
    {
        _first = ref first;
        _length = 1;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BigReadOnlySpan{T}"/> struct that represents the entire <see cref="BigArray{T}"/>.
    /// </summary>
    /// <param name="array">The <see cref="BigArray{T}"/> to represent.</param>
    public BigReadOnlySpan(BigArray<T> array)
    {
        _first = ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array._storage));
        _length = array._length;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BigReadOnlySpan{T}"/> struct that represents a span of memory starting at the specified pointer and with the specified length.
    /// </summary>
    /// <param name="pointer">A pointer to the first element in the span.</param>
    /// <param name="length">The number of elements in the span.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the length is negative.</exception>
#pragma warning disable CS8500
    public unsafe BigReadOnlySpan(T* pointer, nint length)
#pragma warning restore CS8500
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");
        _first = ref Unsafe.AsRef<T>(pointer);
        _length = length;
    }

    internal BigReadOnlySpan(ref T first, nint length)
    {
        _first = ref first;
        _length = length;
    }

    /// <summary>
    /// Creates a new <see cref="BigReadOnlySpan{T}"/> that represents a slice of the current span starting at the specified index.
    /// </summary>
    /// <param name="start">The index at which to start the slice.</param>
    /// <returns>A new <see cref="BigReadOnlySpan{T}"/> that represents the specified slice.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BigReadOnlySpan<T> Slice(nint start)
    {
        if ((nuint)start > (nuint)_length) BigArray<T>.ThrowOutOfRangeException(start);
        return new BigReadOnlySpan<T>(ref Unsafe.Add(ref Unsafe.AsRef(in _first), start), _length - start);
    }

    /// <summary>
    /// Creates a new <see cref="BigReadOnlySpan{T}"/> that represents a slice of the current span starting at the specified index and with the specified length.
    /// </summary>
    /// <param name="start">The index at which to start the slice.</param>
    /// <param name="length">The number of elements in the slice.</param>
    /// <returns>A new <see cref="BigReadOnlySpan{T}"/> that represents the specified slice.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BigReadOnlySpan<T> Slice(nint start, nint length)
    {
        if ((nuint)start > (nuint)_length || (nuint)length > (nuint)(_length - start)) BigArray<T>.ThrowOutOfRangeException(start);
        return new BigReadOnlySpan<T>(ref Unsafe.Add(ref Unsafe.AsRef(in _first), start), length);
    }

    /// <summary>
    /// Gets a read-only reference to the element at the specified index in the span.
    /// </summary>
    /// <param name="index">The index of the element to retrieve.</param>
    /// <returns>A read-only reference to the element at the specified index.</returns>
    public readonly ref readonly T this[nint index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((nuint)index >= (nuint)_length) BigArray<T>.ThrowOutOfRangeException(index);
            return ref Unsafe.Add(ref Unsafe.AsRef(in _first), index);
        }
    }

    /// <summary>
    /// Gets a read-only reference to the element that can be used for pinning.
    /// </summary>
    /// <returns>A read-only reference to the first element of the span.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T GetPinnableReference()
    {
        return ref _first;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="BigReadOnlySpan{T}"/>.
    /// </summary>
    /// <returns>An enumerator that iterates through the <see cref="BigReadOnlySpan{T}"/>.</returns>
    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    /// <summary>
    /// Enumerates the elements of a <see cref="BigReadOnlySpan{T}"/>.
    /// </summary>
    public ref struct Enumerator : IEnumerator<T>
    {
        private readonly BigReadOnlySpan<T> _span;
        private nint _offset;
        internal Enumerator(BigReadOnlySpan<T> span)
        {
            _span = span;
            _offset = -1;
        }

        /// <inheritdoc/>
        public readonly ref readonly T Current => ref _span[_offset];

        readonly T IEnumerator<T>.Current => Current;
        readonly object? IEnumerator.Current => Current;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (_offset < _span._length - 1)
            {
                _offset++;
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public readonly void Dispose() { }

        /// <inheritdoc/>
        public void Reset()
        {
            _offset = -1;
        }
    }
}
