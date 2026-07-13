using System.Collections;
using System.Runtime.CompilerServices;

namespace Hezium.Memory;

public sealed partial class BigArray<T>
{
    internal struct Enumerator : IEnumerator<T>
    {
        private readonly BigArray<T> _array;
        private nint _offset;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(BigArray<T> array)
        {
            _array = array;
            _offset = -1;
        }

        public readonly T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((nuint)_offset >= (nuint)_array._length) ThrowHelpers.ThrowInvalidOperation("Enumeration has either not started or has already finished.");
                return Unsafe.Add(ref MemoryExtensions.GetBigArrayDataReference(_array), _offset);
            }
        }

        readonly object? IEnumerator.Current => Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Dispose() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            nint index = _offset + 1;
            if ((nuint)index >= (nuint)_array._length)
            {
                _offset = _array._length;
                return false;
            }

            _offset = index;
            return true;
        }

        public void Reset()
        {
            _offset = -1;
        }
    }
}
