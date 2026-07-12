using System.Buffers;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hezium.Memory.Tests;

public sealed class CoreApiTests
{
    private static readonly int[] expected = new[] { 1, 2, 3, 4, 5 };

    [Fact]
    public void BigArray_CoreApis_Work()
    {
        Assert.True(BigArray<int>.Empty.IsEmpty);
        Assert.Equal(0, BigArray<int>.Empty.Length);
        Assert.True(BigArray<byte>.MaxLength >= Array.MaxLength);

        Assert.Throws<ArgumentOutOfRangeException>(() => new BigArray<int>(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BigArray<byte>(BigArray<byte>.MaxLength + 1));

        BigArray<int> array = new(5);
        Assert.False(array.IsEmpty);
        Assert.Equal(5, array.Length);

        BigSpan<int> values = [1, 2, 3, 4, 5];

        for (nint i = 0; i < array.Length; i++)
        {
            array[i] = (int)(i + 1);
        }

        Assert.Equal(1, array[0]);
        Assert.Equal(5, array[4]);
        Assert.Throws<ArgumentOutOfRangeException>(() => array[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => array[5]);
        Assert.Equal([1, 2, 3, 4, 5], array.ToArray());
        Assert.Equal([2, 3, 4], array.AsSpan(1, 3).ToArray());

        BigSpan<int> span = array.AsBigSpan();
        Assert.Equal(5, span.Length);
        Assert.Equal([3, 4, 5], ToArray(array.AsBigSpan(2)));
        Assert.Equal([2, 3], ToArray(array.AsBigSpan(1, 2)));

        List<int> enumerated = [];
        foreach (int value in array)
        {
            enumerated.Add(value);
        }

        Assert.Equal(expected, enumerated);

        IEnumerator enumerator = ((IEnumerable)array).GetEnumerator();
        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current);
        enumerator.Reset();
        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current);

        BigArray<int> empty = new(0);
        Assert.True(Unsafe.IsNullRef(ref empty.GetPinnableReference()));
        Assert.True(Unsafe.IsNullRef(ref empty.AsBigSpan().GetPinnableReference()));
        Assert.True(Unsafe.IsNullRef(in ((BigReadOnlySpan<int>)empty.AsBigSpan()).GetPinnableReference()));
    }

    [Fact]
    public void Enumerators_RejectCurrentOutsideTheValidRange()
    {
        IEnumerator<int> arrayEnumerator = new BigArray<int>(1).GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => arrayEnumerator.Current);
        Assert.True(arrayEnumerator.MoveNext());
        Assert.False(arrayEnumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => arrayEnumerator.Current);

        Assert.Throws<InvalidOperationException>(ReadBigSpanEnumeratorBeforeStart);
        Assert.Throws<InvalidOperationException>(ReadBigSpanEnumeratorAfterEnd);
        Assert.Throws<InvalidOperationException>(ReadBigReadOnlySpanEnumeratorBeforeStart);
        Assert.Throws<InvalidOperationException>(ReadBigReadOnlySpanEnumeratorAfterEnd);
    }

    [Fact]
    public void BigArraySpanConstructors_RejectNull()
    {
        Assert.Throws<ArgumentNullException>(CreateBigSpanFromNullBigArray);
        Assert.Throws<ArgumentNullException>(CreateBigReadOnlySpanFromNullBigArray);
    }

    [Fact]
    public void MutableArrayViews_RejectCovariantArrays()
    {
        object[] covariantArray = new string[1];

        Assert.Throws<ArrayTypeMismatchException>(CreateBigSpanFromCovariantArray);
        Assert.Throws<ArrayTypeMismatchException>(CreateBigSpanRangeFromCovariantArray);
        Assert.Throws<ArrayTypeMismatchException>(() => new BigMemory<object>(covariantArray));
        Assert.Throws<ArrayTypeMismatchException>(() => new BigMemory<object>(covariantArray, 0, 1));
    }

    [Fact]
    public void BigArray_ExtensionApis_Work()
    {
        BigArray<int> array = new(5);
        array.Fill(7);
        Assert.Equal([7, 7, 7, 7, 7], array.ToArray());

        array.Clear();
        Assert.Equal([0, 0, 0, 0, 0], array.ToArray());

        for (nint i = 0; i < array.Length; i++)
        {
            array[i] = (int)(i + 1);
        }

        BigArray<int> destination = new(7);
        array.CopyTo(destination, 1);
        Assert.Equal([0, 1, 2, 3, 4, 5, 0], destination.ToArray());

        BigArray<int> exact = new(5);
        array.CopyTo(exact);
        Assert.True(array.TryCopyTo(exact));
        Assert.False(array.TryCopyTo(new BigArray<int>(4)));
        Assert.Equal([1, 2, 3, 4, 5], exact.ToArray());

        Assert.Equal(2, array.IndexOf(3));
        Assert.Equal(2, array.LastIndexOf(3));
        Assert.True(array.Contains(4));
        Assert.False(array.Contains(6));
        Assert.Equal(3, array.BinarySearch(4));
        Assert.Equal(3, array.BinarySearch(4, Comparer<int>.Default));

        BigArray<int?> array2 = new(7);
        array2.Fill(null);
        array2[3] = 42;
        Assert.Equal([null, null, null, 42, null, null, null], array2.ToArray());

        BigArray<int> bigArray = new(8);
        for (int i = 0; i < bigArray.Length; ++i)
        {
            bigArray[i] = i * 2;
        }

        unsafe
        {
            fixed (int* ttPtr = bigArray)
            {
                for (int i = 0; i < bigArray.Length; ++i)
                {
                    Assert.Equal(bigArray[i], *(ttPtr + i));
                }
            }
        }

        BigArray<int> empty = new(0);
        Assert.False(Unsafe.IsNullRef(ref MemoryMarshal.GetBigArrayDataReference(empty)));
        Assert.False(Unsafe.IsNullRef(ref MemoryMarshal.GetReference(empty.AsBigSpan())));
        Assert.False(Unsafe.IsNullRef(in MemoryMarshal.GetReference((BigReadOnlySpan<int>)empty.AsBigSpan())));
    }

    [Fact]
    public unsafe void BigSpan_CoreApis_Work()
    {
        Assert.True(BigSpan<int>.Empty.IsEmpty);
        Assert.Equal(0, BigSpan<int>.Empty.Length);

        int value = 41;
        BigSpan<int> single = new(ref value);
        single[0]++;
        Assert.Equal(42, value);
        Assert.Equal(42, single.GetPinnableReference());

        Span<int> source = [1, 2, 3, 4];
        BigSpan<int> span = source;
        Assert.False(span.IsEmpty);
        Assert.Equal(4, span.Length);
        Assert.Equal(3, span[2]);
        Assert.Equal([2, 3, 4], ToArray(span.Slice(1)));
        Assert.Equal([2, 3], ToArray(span.Slice(1, 2)));
        Assert.Throws<ArgumentOutOfRangeException>(SliceBigSpanPastEnd);
        Assert.Throws<ArgumentOutOfRangeException>(IndexBigSpanPastEnd);

        BigSpan<int>.Enumerator enumerator = span.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current);
        enumerator.Reset();
        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current);

        fixed (int* pointer = source)
        {
            BigSpan<int> pointerSpan = new(pointer, source.Length);
            Assert.Equal([1, 2, 3, 4], ToArray(pointerSpan));
            Assert.Throws<ArgumentOutOfRangeException>(CreateBigSpanWithNegativePointerLength);
        }
    }

    private static readonly int[] valuesArray = new[] { 1, 3 };

    [Fact]
    public void BigSpan_ExtensionApis_Work()
    {
        int[] values = [1, 2, 3, 2, 1];
        BigSpan<int> span = values;

        Assert.Equal(1, span.IndexOf(2));
        Assert.Equal(3, span.LastIndexOf(2));
        Assert.True(span.Contains(3));
        Assert.Equal(2, span.IndexOfAny(4, 3));
        Assert.Equal(1, span.IndexOfAny(4, 2, 9));
        Assert.Equal(3, span.LastIndexOfAny(4, 2));
        Assert.Equal(3, span.LastIndexOfAny(4, 2, 9));
        Assert.True(span.ContainsAny(9, 3));
        Assert.True(span.ContainsAny(9, 3, 8));
        Assert.Equal(0, span.IndexOfAnyExcept(2));
        Assert.Equal(2, span.IndexOfAnyExcept(1, 2));
        Assert.Equal(-1, span.IndexOfAnyExcept(1, 2, 3));
        Assert.Equal(4, span.LastIndexOfAnyExcept(2));
        Assert.Equal(2, span.LastIndexOfAnyExcept(1, 2));
        Assert.Equal(-1, span.LastIndexOfAnyExcept(1, 2, 3));
        Assert.True(span.ContainsAnyExcept(2));
        Assert.True(span.ContainsAnyExcept(1, 2));
        Assert.False(span.ContainsAnyExcept(1, 2, 3));

        BigReadOnlySpan<int> needles = new[] { 3, 9 };
        Assert.Equal(2, span.IndexOfAny(needles));
        Assert.Equal(2, span.LastIndexOfAny(needles));
        Assert.True(span.ContainsAny(needles));
        Assert.Equal(1, span.IndexOfAnyExcept(valuesArray));
        Assert.Equal(3, span.LastIndexOfAnyExcept(valuesArray));
        Assert.True(span.ContainsAnyExcept(new[] { 1, 3 }));

        Assert.True(span.SequenceEqual((BigReadOnlySpan<int>)values.AsSpan()));
        Assert.True(span.SequenceEqual((BigSpan<int>)values.AsSpan()));
        Assert.True(span.SequenceCompareTo((BigReadOnlySpan<int>)[1, 2, 4]) < 0);
        Assert.Equal(0, span.SequenceCompareTo((BigSpan<int>)values.AsSpan()));
        Assert.True(span.StartsWith(new[] { 1, 2 }));
        Assert.True(span.EndsWith(new[] { 2, 1 }));
        Assert.Equal(2, span.BinarySearch(3));
        Assert.Equal(2, span.BinarySearch(3, Comparer<int>.Default));

        Assert.Equal(2, span.IndexOfAnyInRange(3, 4));
        Assert.Equal(2, span.LastIndexOfAnyInRange(3, 4));
        Assert.True(span.ContainsAnyInRange(3, 4));
        Assert.Equal(0, span.IndexOfAnyExceptInRange(2, 3));
        Assert.Equal(4, span.LastIndexOfAnyExceptInRange(2, 3));
        Assert.True(span.ContainsAnyExceptInRange(2, 3));
    }

    private static readonly int[] trimElements = new[] { 0, 2 };

    [Fact]
    public void BigSpan_CopyConvertTrimSplitAndSortApis_Work()
    {
        int[] values = [0, 2, 1, 2, 0];
        BigSpan<int> span = values;

        Assert.Equal([2, 1, 2], ToArray(span.Trim(0)));
        Assert.Equal([1], ToArray(span.Trim(trimElements)));
        Assert.Equal([2, 1, 2, 0], ToArray(span.TrimStart(0)));
        Assert.Equal([1, 2, 0], ToArray(span.TrimStart(trimElements)));
        Assert.Equal([0, 2, 1, 2], ToArray(span.TrimEnd(0)));
        Assert.Equal([0, 2, 1], ToArray(span.TrimEnd(new[] { 0, 2 })));

        Assert.Equal([0, 2, 1, 2, 0], span.ToArray());
        Assert.Equal([2, 1, 2], span.ToSpan(1, 3).ToArray());
        Assert.Equal([0, 2, 1, 2, 0], span.ToBigArray().ToArray());

        int[] copy = new int[5];
        span.CopyTo(copy);
        Assert.Equal(values, copy);

        int[] overlappingCopy = [1, 2, 3, 4, 5, 6];
        ((BigSpan<int>)overlappingCopy.AsSpan(0, 5)).CopyTo(overlappingCopy.AsSpan(1));
        Assert.Equal([1, 1, 2, 3, 4, 5], overlappingCopy);

        overlappingCopy = [1, 2, 3, 4, 5, 6];
        ((BigSpan<int>)overlappingCopy.AsSpan(1, 5)).CopyTo(overlappingCopy.AsSpan(0, 5));
        Assert.Equal([2, 3, 4, 5, 6, 6], overlappingCopy);

        BigSpan<int>.Empty.CopyTo(Span<int>.Empty);
        Assert.True(BigSpan<int>.Empty.TryCopyTo(Span<int>.Empty));

        int[] shortCopy = new int[4];
        Assert.False(span.TryCopyTo(shortCopy));
        Assert.True(span.TryCopyTo(copy));

        BigArray<int> destinationArray = new(5);
        span.CopyTo(destinationArray.AsBigSpan());
        Assert.True(span.TryCopyTo(destinationArray.AsBigSpan()));
        Assert.Equal(values, destinationArray.ToArray());

        Assert.Throws<ArgumentException>(CopyBigSpanToShortSpan);
        Assert.Throws<ArgumentException>(CopyBigSpanToShortBigSpan);

        Assert.Equal([1, 1, 1], SegmentLengths(span.Split(2)));
        Assert.Equal([0, 0, 1, 0, 0], SegmentLengths(span.SplitAny(new[] { 0, 2 })));

        byte[] byteValues = [0, 2, 1, 2, 0];
        BigSpan<byte> byteSpan = byteValues;
        SearchValues<byte> searchValues = SearchValues.Create([2, 9]);
        Assert.Equal([1, 1, 1], SegmentLengths(byteSpan.SplitAny(searchValues)));
        Assert.Equal(1, byteSpan.IndexOfAny(searchValues));
        Assert.Equal(3, byteSpan.LastIndexOfAny(searchValues));
        Assert.True(byteSpan.ContainsAny(searchValues));
        Assert.Equal(0, byteSpan.IndexOfAnyExcept(searchValues));
        Assert.Equal(4, byteSpan.LastIndexOfAnyExcept(searchValues));
        Assert.True(byteSpan.ContainsAnyExcept(searchValues));
        Assert.Throws<ArgumentNullException>(IndexBigSpanOfNullSearchValues);

        int[] sortable = [3, 1, 2];
        ((BigSpan<int>)sortable.AsSpan()).Sort();
        Assert.Equal([1, 2, 3], sortable);

        sortable = [1, 3, 2];
        ((BigSpan<int>)sortable.AsSpan()).Sort(Comparer<int>.Create((left, right) => right.CompareTo(left)));
        Assert.Equal([3, 2, 1], sortable);

        sortable = [2, 3, 1];
        ((BigSpan<int>)sortable.AsSpan()).Sort((left, right) => left.CompareTo(right));
        Assert.Equal([1, 2, 3], sortable);
        Assert.Throws<ArgumentNullException>(() => ((BigSpan<int>)sortable.AsSpan()).Sort((Comparison<int>)null!));

        span.Clear();
        Assert.Equal([0, 0, 0, 0, 0], values);

        span.Fill(4);
        Assert.Equal([4, 4, 4, 4, 4], values);
    }

    private static readonly int[] valueArray = new[] { 1, 2 };

    [Fact]
    public unsafe void BigReadOnlySpan_CoreAndExtensionApis_Work()
    {
        Assert.True(BigReadOnlySpan<int>.Empty.IsEmpty);
        Assert.Equal(0, BigReadOnlySpan<int>.Empty.Length);

        int value = 42;
        BigReadOnlySpan<int> single = new(ref value);
        Assert.Equal(1, single.Length);
        Assert.Equal(42, single[0]);
        Assert.Equal(42, single.GetPinnableReference());

        int[] values = [1, 2, 3, 2, 1];
        BigReadOnlySpan<int> span = values;
        Assert.False(span.IsEmpty);
        Assert.Equal(5, span.Length);
        Assert.Equal([2, 3, 2, 1], ToArray(span.Slice(1)));
        Assert.Equal([2, 3], ToArray(span.Slice(1, 2)));
        Assert.Throws<ArgumentOutOfRangeException>(SliceBigReadOnlySpanPastEnd);
        Assert.Throws<ArgumentOutOfRangeException>(IndexBigReadOnlySpanPastEnd);

        BigReadOnlySpan<int>.Enumerator enumerator = span.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current);
        enumerator.Reset();
        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current);

        fixed (int* pointer = values)
        {
            BigReadOnlySpan<int> pointerSpan = new(pointer, values.Length);
            Assert.Equal(values, ToArray(pointerSpan));
            Assert.Throws<ArgumentOutOfRangeException>(CreateBigReadOnlySpanWithNegativePointerLength);
        }

        Assert.Equal(values, span.ToArray());
        Assert.Equal(values, span.ToBigArray().ToArray());
        Assert.Equal([2, 3, 2], span.ToSpan(1, 3).ToArray());

        int[] destination = new int[5];
        span.CopyTo(destination);
        Assert.Equal(values, destination);
        Assert.True(span.TryCopyTo(destination));
        Assert.False(span.TryCopyTo(new int[4]));

        BigArray<int> destinationArray = new(5);
        span.CopyTo(destinationArray.AsBigSpan());
        Assert.True(span.TryCopyTo(destinationArray.AsBigSpan()));
        Assert.Equal(values, destinationArray.ToArray());
        Assert.Throws<ArgumentException>(CopyBigReadOnlySpanToShortSpan);
        Assert.Throws<ArgumentException>(CopyBigReadOnlySpanToShortBigSpan);

        Assert.Equal(1, span.IndexOf(2));
        Assert.Equal(3, span.LastIndexOf(2));
        Assert.True(span.Contains(3));
        Assert.Equal(2, span.BinarySearch(3));
        Assert.Equal(2, span.BinarySearch(3, Comparer<int>.Default));
        Assert.Equal(2, span.IndexOfAny([3, 9]));
        Assert.Equal(2, span.LastIndexOfAny([3, 9]));
        Assert.True(span.ContainsAny([3, 9]));
        Assert.Equal(1, span.IndexOfAny(4, 2));
        Assert.Equal(1, span.IndexOfAny(4, 2, 9));
        Assert.Equal(3, span.LastIndexOfAny(4, 2));
        Assert.Equal(3, span.LastIndexOfAny(4, 2, 9));
        Assert.True(span.ContainsAny(4, 2));
        Assert.True(span.ContainsAny(4, 2, 9));
        Assert.Equal(0, span.IndexOfAnyExcept(2));
        Assert.Equal(2, span.IndexOfAnyExcept(1, 2));
        Assert.Equal(-1, span.IndexOfAnyExcept(1, 2, 3));
        Assert.Equal(4, span.LastIndexOfAnyExcept(2));
        Assert.Equal(2, span.LastIndexOfAnyExcept(1, 2));
        Assert.Equal(-1, span.LastIndexOfAnyExcept(1, 2, 3));
        Assert.True(span.ContainsAnyExcept(2));
        Assert.True(span.ContainsAnyExcept([1, 3]));
        Assert.True(span.ContainsAnyExcept(1, 2));
        Assert.False(span.ContainsAnyExcept(1, 2, 3));
        Assert.True(span.SequenceEqual((BigReadOnlySpan<int>)values));
        Assert.True(span.SequenceCompareTo([1, 2, 4]) < 0);
        Assert.True(span.StartsWith(valueArray));
        Assert.True(span.EndsWith(new[] { 2, 1 }));
        Assert.Equal([1, 1, 1], SegmentLengths(span.Split(2)));
        Assert.Equal([0, 0, 1, 0, 0], SegmentLengths(span.SplitAny(new[] { 1, 2 })));
        Assert.Equal([3], ToArray(((BigReadOnlySpan<int>)[0, 3, 0]).Trim(0)));
        Assert.Equal([3], ToArray(((BigReadOnlySpan<int>)[0, 2, 3, 2, 0]).Trim(new[] { 0, 2 })));
        Assert.Equal([3, 0], ToArray(((BigReadOnlySpan<int>)[0, 3, 0]).TrimStart(0)));
        Assert.Equal([3, 2, 0], ToArray(((BigReadOnlySpan<int>)[0, 2, 3, 2, 0]).TrimStart(new[] { 0, 2 })));
        Assert.Equal([0, 3], ToArray(((BigReadOnlySpan<int>)[0, 3, 0]).TrimEnd(0)));
        Assert.Equal([0, 2, 3], ToArray(((BigReadOnlySpan<int>)[0, 2, 3, 2, 0]).TrimEnd(new[] { 0, 2 })));

        byte[] byteValues = [1, 2, 3, 2, 1];
        BigReadOnlySpan<byte> byteSpan = byteValues;
        SearchValues<byte> searchValues = SearchValues.Create([2, 9]);
        Assert.Equal([1, 1, 1], SegmentLengths(byteSpan.SplitAny(searchValues)));
        Assert.Equal(1, byteSpan.IndexOfAny(searchValues));
        Assert.Equal(3, byteSpan.LastIndexOfAny(searchValues));
        Assert.True(byteSpan.ContainsAny(searchValues));
        Assert.Equal(0, byteSpan.IndexOfAnyExcept(searchValues));
        Assert.Equal(4, byteSpan.LastIndexOfAnyExcept(searchValues));
        Assert.True(byteSpan.ContainsAnyExcept(searchValues));
        Assert.Throws<ArgumentNullException>(IndexBigReadOnlySpanOfNullSearchValues);

        Assert.Equal(2, span.IndexOfAnyInRange(3, 4));
        Assert.Equal(2, span.LastIndexOfAnyInRange(3, 4));
        Assert.True(span.ContainsAnyInRange(3, 4));
        Assert.Equal(0, span.IndexOfAnyExceptInRange(2, 3));
        Assert.Equal(4, span.LastIndexOfAnyExceptInRange(2, 3));
        Assert.True(span.ContainsAnyExceptInRange(2, 3));
    }

    [Fact]
    public unsafe void BigMemory_CoreApis_Work()
    {
        Assert.True(BigMemory<int>.Empty.IsEmpty);
        Assert.Equal(0, BigMemory<int>.Empty.Length);

        BigMemory<int> nullMemory = new((int[]?)null);
        Assert.True(nullMemory.IsEmpty);
        Assert.Equal(0, nullMemory.Length);
        _ = new BigMemory<int>((int[]?)null, 0, 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => new BigMemory<int>((int[]?)null, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BigMemory<int>(new int[1], 0, 2));

        int[] values = [1, 2, 3, 4, 5];
        BigMemory<int> memory = values;
        Assert.False(memory.IsEmpty);
        Assert.Equal(5, memory.Length);
        Assert.Equal([2, 3, 4], memory.Slice(1, 3).ToArray());
        Assert.Equal([3, 4, 5], memory.Slice(2).ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(() => memory.Slice(6));
        Assert.Throws<ArgumentOutOfRangeException>(() => memory.Slice(4, 2));

        memory.Span[2] = 30;
        Assert.Equal(30, values[2]);

        BigMemory<int> segmentMemory = new ArraySegment<int>(values, 1, 3);
        Assert.Equal([2, 30, 4], segmentMemory.ToArray());

        BigArray<int> owner = new(5);
        for (nint i = 0; i < owner.Length; i++)
        {
            owner[i] = (int)(i + 1);
        }

        BigMemory<int> ownerMemory = owner.AsBigMemory(1, 3);
        Assert.Equal([2, 3, 4], ownerMemory.ToArray());
        ownerMemory.Span[1] = 42;
        Assert.Equal(42, owner[2]);
        Assert.Equal([2, 42, 4], ((BigReadOnlyMemory<int>)owner.AsBigMemory(1, 3)).ToArray());

        BigMemory<int> destination = new int[5];
        memory.CopyTo(destination);
        Assert.Equal(values, destination.ToArray());
        Assert.True(memory.TryCopyTo(destination));
        Assert.False(memory.TryCopyTo(new int[4]));
        Assert.Throws<ArgumentException>(() => memory.CopyTo(new int[4]));
        Assert.Equal(values, memory.ToBigArray().ToArray());

        BigReadOnlyMemory<int> readOnly = memory;
        Assert.Equal(values, readOnly.ToArray());
        Assert.True(memory.Equals(new BigMemory<int>(values)));
        Assert.True(memory.Equals((object)new BigMemory<int>(values)));
        Assert.False(memory.Equals(new BigMemory<int>(values, 1, 4)));
        Assert.Equal(memory.GetHashCode(), new BigMemory<int>(values).GetHashCode());

        int[] pinnedValues = [1, 2, 3];
        BigMemory<int> pinnedMemory = new(pinnedValues, 1, 1);
        using (MemoryHandle handle = pinnedMemory.Pin())
        {
            *(int*)handle.Pointer = 99;
        }

        Assert.Equal(99, pinnedValues[1]);

        BigArray<int> pinnedOwner = new(2);
        using (MemoryHandle handle = pinnedOwner.AsBigMemory(1, 1).Pin())
        {
            *(int*)handle.Pointer = 77;
        }

        Assert.Equal(77, pinnedOwner[1]);

        using (BigMemory<string>.Empty.Pin())
        {
        }

        Assert.Throws<ArgumentException>(() => new BigMemory<string>(["value"]).Pin());
        Assert.Throws<ArgumentException>(() => new BigMemory<string>(["value"], 1, 0).Pin());
    }

    [Fact]
    public unsafe void BigReadOnlyMemory_CoreApis_Work()
    {
        Assert.True(BigReadOnlyMemory<int>.Empty.IsEmpty);
        Assert.Equal(0, BigReadOnlyMemory<int>.Empty.Length);

        BigReadOnlyMemory<int> nullMemory = new((int[]?)null);
        Assert.True(nullMemory.IsEmpty);
        Assert.Equal(0, nullMemory.Length);
        _ = new BigReadOnlyMemory<int>((int[]?)null, 0, 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => new BigReadOnlyMemory<int>((int[]?)null, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BigReadOnlyMemory<int>(new int[1], 0, 2));

        int[] values = [1, 2, 3, 4, 5];
        BigReadOnlyMemory<int> memory = values;
        Assert.False(memory.IsEmpty);
        Assert.Equal(5, memory.Length);
        Assert.Equal([2, 3, 4], memory.Slice(1, 3).ToArray());
        Assert.Equal([3, 4, 5], memory.Slice(2).ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(() => memory.Slice(6));
        Assert.Throws<ArgumentOutOfRangeException>(() => memory.Slice(4, 2));

        BigReadOnlyMemory<int> segmentMemory = new ArraySegment<int>(values, 1, 3);
        Assert.Equal([2, 3, 4], segmentMemory.ToArray());

        BigArray<int> owner = new(5);
        for (nint i = 0; i < owner.Length; i++)
        {
            owner[i] = (int)(i + 1);
        }

        BigReadOnlyMemory<int> ownerMemory = new(owner, 1, 3);
        Assert.Equal([2, 3, 4], ownerMemory.ToArray());
        Assert.Equal([2, 3, 4], new BigReadOnlyMemory<int>(owner, 1, 3).ToArray());

        BigMemory<int> destination = new int[5];
        memory.CopyTo(destination);
        Assert.Equal(values, destination.ToArray());
        Assert.True(memory.TryCopyTo(destination));
        Assert.False(memory.TryCopyTo(new int[4]));
        Assert.Throws<ArgumentException>(() => memory.CopyTo(new int[4]));
        Assert.Equal(values, memory.ToBigArray().ToArray());

        Assert.True(memory.Equals(new BigReadOnlyMemory<int>(values)));
        Assert.True(memory.Equals((object)new BigReadOnlyMemory<int>(values)));
        Assert.False(memory.Equals(new BigReadOnlyMemory<int>(values, 1, 4)));
        Assert.Equal(memory.GetHashCode(), new BigReadOnlyMemory<int>(values).GetHashCode());

        int[] pinnedValues = [1, 2, 3];
        BigReadOnlyMemory<int> pinnedMemory = new(pinnedValues, 1, 1);
        using (MemoryHandle handle = pinnedMemory.Pin())
        {
            Assert.Equal(2, *(int*)handle.Pointer);
        }

        Assert.Throws<ArgumentException>(() => new BigReadOnlyMemory<string>(["value"]).Pin());
        Assert.Throws<ArgumentException>(() => new BigReadOnlyMemory<string>(["value"], 1, 0).Pin());
    }

    [Fact]
    public void BigCharMemoryAndSpan_TextRequiresSmallSpanWindow()
    {
        char[] chars = "hello".ToCharArray();

        BigSpan<char> span = chars;
        Assert.Equal("hello", span.ToSpan(0, 5).ToString());
        Assert.Equal("ell", span.Slice(1, 3).ToSpan(0, 3).ToString());

        BigReadOnlySpan<char> readOnlySpan = chars;
        Assert.Equal("hello", readOnlySpan.ToSpan(0, 5).ToString());
        Assert.Equal("ell", readOnlySpan.Slice(1, 3).ToSpan(0, 3).ToString());

        BigMemory<char> memory = chars;
        Assert.Equal("ell", memory.Slice(1, 3).Span.ToSpan(0, 3).ToString());

        BigReadOnlyMemory<char> readOnlyMemory = chars;
        Assert.Equal("ell", readOnlyMemory.Slice(1, 3).Span.ToSpan(0, 3).ToString());
    }

    [Fact]
    public void MemoryMarshalExtensionApis_Work()
    {
        int value = 42;
        BigSpan<int> span = MemoryMarshal.CreateBigSpan(ref value, 1);
        Assert.Equal(1, span.Length);
        Assert.Equal(42, MemoryMarshal.GetReference(span));

        ref int writable = ref MemoryMarshal.GetReference(span);
        writable = 43;
        Assert.Equal(43, value);

        BigReadOnlySpan<int> readOnlySpan = span;
        Assert.Equal(43, MemoryMarshal.GetReference(readOnlySpan));
        Assert.Throws<ArgumentOutOfRangeException>(() => MemoryMarshal.CreateBigSpan(ref value, -1));
    }

    [Fact]
    public void LargeLengthContract_IsExposedWithoutHugeAllocation()
    {
        Assert.True(BigArray<byte>.MaxLength >= Array.MaxLength);

        BigArray<byte> array = new(1024 * 1024);
        nint highIndex = array.Length - 1;
        array[highIndex] = 123;

        Assert.Equal(123, array[highIndex]);
        Assert.Equal(123, array.AsBigSpan(highIndex, 1)[0]);
    }

    [Fact]
    public void BigSpan_Algorithms_LargeLengthAboveIntMaxValue()
    {
        if (!Environment.Is64BitProcess)
        {
            return;
        }

        const long LargeLengthValue = 3_000_000_000L;
        nuint byteLength = checked((nuint)LargeLengthValue);
        nint length = (nint)byteLength;
        BigArray<byte> array = new(length);
        BigSpan<byte> span = array.AsBigSpan();
        nint chunkBoundary = Array.MaxLength;
        nint lastIndex = length - 1;

        span[lastIndex] = 0x01;

        Assert.Equal(lastIndex, span.BinarySearch((byte)0x01));
        Assert.Equal(lastIndex, span.BinarySearch((byte)0x01, Comparer<byte>.Default));
        Assert.Equal(~length, span.BinarySearch((byte)0x02));
        Assert.Equal(~length, span.BinarySearch((byte)0x02, Comparer<byte>.Default));

        BigReadOnlySpan<byte> readOnlySpan = span;
        Assert.Equal(lastIndex, readOnlySpan.BinarySearch((byte)0x01));
        Assert.Equal(~length, readOnlySpan.BinarySearch((byte)0x02, Comparer<byte>.Default));

        span[0] = 0x11;
        span[chunkBoundary - 1] = 0x22;
        span[chunkBoundary] = 0x33;
        span[lastIndex] = 0x44;

        Assert.Equal(length, span.Length);
        Assert.Equal(0x22, span[chunkBoundary - 1]);
        Assert.Equal(0x33, span[chunkBoundary]);

        span.CopyTo(span);

        Assert.True(span.TryCopyTo(span));
        Assert.True(span.SequenceEqual(span));
        Assert.Equal(0, span.SequenceCompareTo(span));
        Assert.True(span.StartsWith(span));
        Assert.True(span.EndsWith(span));
        Assert.Equal(0, span.IndexOf((byte)0x11));
        Assert.Equal(lastIndex, span.LastIndexOf((byte)0x44));
        Assert.Equal(0, span.IndexOfAny((byte)0x11, (byte)0x55));
        Assert.Equal(lastIndex, span.LastIndexOfAny((byte)0x44, (byte)0x55));
        Assert.Equal(0, span.IndexOfAnyExcept((byte)0x00));
        Assert.Equal(lastIndex, span.LastIndexOfAnyExcept((byte)0x00));
        Assert.Equal(0, span.IndexOfAnyInRange((byte)0x10, (byte)0x12));
        Assert.Equal(lastIndex, span.LastIndexOfAnyInRange((byte)0x40, (byte)0x45));

        InvalidOperationException? exception = null;
        try
        {
            _ = span.ToArray();
        }
        catch (InvalidOperationException caught)
        {
            exception = caught;
        }

        Assert.NotNull(exception);
    }

    [Fact]
    public void GC_BigArrayAllocationApis_Work()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GC.AllocateBigArray<int>(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => GC.AllocateUninitializedBigArray<byte>(BigArray<byte>.MaxLength + 1));

        BigArray<int> initialized = GC.AllocateBigArray<int>(3);

        Assert.Equal(3, initialized.Length);
        Assert.Equal([0, 0, 0], initialized.ToArray());

        BigArray<int> uninitialized = GC.AllocateUninitializedBigArray<int>(3);

        Assert.Equal(3, uninitialized.Length);
        uninitialized[2] = 42;
        Assert.Equal(42, uninitialized[2]);

        BigArray<int> pinned = GC.AllocateBigArray<int>(3, pinned: true);

        Assert.Equal(3, pinned.Length);
        pinned[2] = 43;
        Assert.Equal(43, pinned[2]);
    }

    [Fact]
    public void BigArray_ChunkFactory_CoversEverySupportedElementSize()
    {
        HashSet<int> chunkLengths = [];

        for (int elementSize = 1; elementSize <= 65535; elementSize++)
        {
            int chunkLength = 65535 / elementSize;

            Assert.True((long)chunkLength * elementSize <= 65535);

            if (chunkLengths.Add(chunkLength))
            {
                var allocate = BigArray<byte>.CreateBigArrayAllocator(chunkLength);
                Array storage = allocate(1, false, false);

                Assert.Single(storage);
                Assert.Equal(chunkLength, GetStorageChunkLength<byte>(storage));
            }
        }

        Assert.Equal(510, chunkLengths.Count);

        int referenceChunkLength = 65535 / Unsafe.SizeOf<object>();
        var referenceAllocate = BigArray<object>.CreateBigArrayAllocator(referenceChunkLength);
        Array referenceStorage = referenceAllocate(0, false, false);

        Assert.Empty(referenceStorage);
        Assert.Equal(referenceChunkLength, GetStorageChunkLength<object>(referenceStorage));
    }

    [Fact]
    public void BigArray_ChunkFactory_LoadsArrayTypesOnUse()
    {
        var allocate = BigArray<MaxSizedElement>.CreateBigArrayAllocator(1);

        Assert.Empty(allocate(0, false, false));
    }

    [InlineArray(65535)]
    private struct MaxSizedElement
    {
        private byte _first;
    }

    private static int GetStorageChunkLength<TElement>(Array storage)
    {
        Type? chunkType = storage.GetType().GetElementType();
        Assert.NotNull(chunkType);

        int chunkLength = 1;
        while (chunkType != typeof(TElement))
        {
            Assert.True(chunkType.IsGenericType);

            InlineArrayAttribute? inlineArray = chunkType.GetCustomAttribute<InlineArrayAttribute>();
            Assert.NotNull(inlineArray);
            chunkLength = checked(chunkLength * inlineArray.Length);

            Type[] typeArguments = chunkType.GetGenericArguments();
            Assert.Single(typeArguments);
            chunkType = typeArguments[0];
        }

        return chunkLength;
    }

    private static T[] ToArray<T>(BigSpan<T> span)
    {
        T[] result = new T[(int)span.Length];
        for (nint i = 0; i < span.Length; i++)
        {
            result[(int)i] = span[i];
        }

        return result;
    }

    private static T[] ToArray<T>(BigReadOnlySpan<T> span)
    {
        T[] result = new T[(int)span.Length];
        for (nint i = 0; i < span.Length; i++)
        {
            result[(int)i] = span[i];
        }

        return result;
    }

    private static int[] SegmentLengths<T>(BigSpanSplitEnumerator<T> enumerator)
    {
        List<int> result = [];
        while (enumerator.MoveNext())
        {
            result.Add((int)enumerator.Current.Length);
        }

        return [.. result];
    }

    private static int[] SegmentLengths<T>(BigSpanSearchValuesSplitEnumerator<T> enumerator)
        where T : IEquatable<T>
    {
        List<int> result = [];
        while (enumerator.MoveNext())
        {
            result.Add((int)enumerator.Current.Length);
        }

        return [.. result];
    }

    private static void SliceBigSpanPastEnd()
    {
        BigSpan<int> span = new int[] { 1, 2, 3, 4 };
        span.Slice(5);
    }

    private static void ReadBigSpanEnumeratorBeforeStart()
    {
        BigSpan<int>.Enumerator enumerator = ((BigSpan<int>)new int[1]).GetEnumerator();
        _ = enumerator.Current;
    }

    private static void ReadBigSpanEnumeratorAfterEnd()
    {
        BigSpan<int>.Enumerator enumerator = ((BigSpan<int>)new int[1]).GetEnumerator();
        Assert.True(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
        _ = enumerator.Current;
    }

    private static void CreateBigSpanFromNullBigArray()
    {
        _ = new BigSpan<int>((BigArray<int>)null!);
    }

    private static void CreateBigSpanFromCovariantArray()
    {
        object[] covariantArray = new string[1];
        _ = new BigSpan<object>(covariantArray);
    }

    private static void CreateBigSpanRangeFromCovariantArray()
    {
        object[] covariantArray = new string[1];
        _ = new BigSpan<object>(covariantArray, 0, 1);
    }

    private static void IndexBigSpanPastEnd()
    {
        BigSpan<int> span = new int[] { 1, 2, 3, 4 };
        _ = span[4];
    }

    private static unsafe void CreateBigSpanWithNegativePointerLength()
    {
        _ = new BigSpan<int>((int*)0, -1);
    }

    private static void CopyBigSpanToShortSpan()
    {
        BigSpan<int> span = new int[] { 0, 2, 1, 2, 0 };
        span.CopyTo(new int[4]);
    }

    private static void CopyBigSpanToShortBigSpan()
    {
        BigSpan<int> span = new int[] { 0, 2, 1, 2, 0 };
        span.CopyTo(new BigArray<int>(4).AsBigSpan());
    }

    private static void IndexBigSpanOfNullSearchValues()
    {
        BigSpan<int> span = new int[] { 0, 2, 1, 2, 0 };
        span.IndexOfAny(null!);
    }

    private static void SliceBigReadOnlySpanPastEnd()
    {
        BigReadOnlySpan<int> span = new int[] { 1, 2, 3, 2, 1 };
        span.Slice(6);
    }

    private static void ReadBigReadOnlySpanEnumeratorBeforeStart()
    {
        BigReadOnlySpan<int>.Enumerator enumerator = ((BigReadOnlySpan<int>)new int[1]).GetEnumerator();
        _ = enumerator.Current;
    }

    private static void ReadBigReadOnlySpanEnumeratorAfterEnd()
    {
        BigReadOnlySpan<int>.Enumerator enumerator = ((BigReadOnlySpan<int>)new int[1]).GetEnumerator();
        Assert.True(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
        _ = enumerator.Current;
    }

    private static void CreateBigReadOnlySpanFromNullBigArray()
    {
        _ = new BigReadOnlySpan<int>((BigArray<int>)null!);
    }

    private static void IndexBigReadOnlySpanPastEnd()
    {
        BigReadOnlySpan<int> span = new int[] { 1, 2, 3, 2, 1 };
        _ = span[5];
    }

    private static unsafe void CreateBigReadOnlySpanWithNegativePointerLength()
    {
        _ = new BigReadOnlySpan<int>((int*)0, -1);
    }

    private static void CopyBigReadOnlySpanToShortSpan()
    {
        BigReadOnlySpan<int> span = new int[] { 1, 2, 3, 2, 1 };
        span.CopyTo(new int[4]);
    }

    private static void CopyBigReadOnlySpanToShortBigSpan()
    {
        BigReadOnlySpan<int> span = new int[] { 1, 2, 3, 2, 1 };
        span.CopyTo(new BigArray<int>(4).AsBigSpan());
    }

    private static void IndexBigReadOnlySpanOfNullSearchValues()
    {
        BigReadOnlySpan<int> span = new int[] { 1, 2, 3, 2, 1 };
        span.IndexOfAny(null!);
    }
}
