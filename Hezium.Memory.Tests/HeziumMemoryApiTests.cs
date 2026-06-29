using System.Buffers;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hezium.Memory.Tests;

public sealed class HeziumMemoryApiTests
{
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

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, enumerated);

        IEnumerator enumerator = ((IEnumerable)array).GetEnumerator();
        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current);
        enumerator.Reset();
        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current);
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
        Assert.Equal(1, span.IndexOfAnyExcept(new[] { 1, 3 }));
        Assert.Equal(3, span.LastIndexOfAnyExcept(new[] { 1, 3 }));
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

    [Fact]
    public void BigSpan_CopyConvertTrimSplitAndSortApis_Work()
    {
        int[] values = [0, 2, 1, 2, 0];
        BigSpan<int> span = values;

        Assert.Equal([2, 1, 2], ToArray(span.Trim(0)));
        Assert.Equal([1], ToArray(span.Trim(new[] { 0, 2 })));
        Assert.Equal([2, 1, 2, 0], ToArray(span.TrimStart(0)));
        Assert.Equal([1, 2, 0], ToArray(span.TrimStart(new[] { 0, 2 })));
        Assert.Equal([0, 2, 1, 2], ToArray(span.TrimEnd(0)));
        Assert.Equal([0, 2, 1], ToArray(span.TrimEnd(new[] { 0, 2 })));

        Assert.Equal([0, 2, 1, 2, 0], span.ToArray());
        Assert.Equal([2, 1, 2], span.ToSpan(1, 3).ToArray());
        Assert.Equal([0, 2, 1, 2, 0], span.ToBigArray().ToArray());

        int[] copy = new int[5];
        span.CopyTo(copy);
        Assert.Equal(values, copy);

        int[] shortCopy = new int[4];
        Assert.False(span.TryCopyTo(shortCopy));
        Assert.True(span.TryCopyTo(copy));

        BigArray<int> destinationArray = new(5);
        span.CopyTo(destinationArray.AsBigSpan());
        Assert.True(span.TryCopyTo(destinationArray.AsBigSpan()));
        Assert.Equal(values, destinationArray.ToArray());

        Assert.Throws<ArgumentOutOfRangeException>(CopyBigSpanToShortSpan);
        Assert.Throws<ArgumentOutOfRangeException>(CopyBigSpanToShortBigSpan);

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
        Assert.Throws<ArgumentOutOfRangeException>(CopyBigReadOnlySpanToShortSpan);
        Assert.Throws<ArgumentOutOfRangeException>(CopyBigReadOnlySpanToShortBigSpan);

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
        Assert.True(span.StartsWith(new[] { 1, 2 }));
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
        Assert.Throws<ArgumentOutOfRangeException>(() => memory.CopyTo(new int[4]));
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
        Assert.Throws<ArgumentOutOfRangeException>(() => memory.CopyTo(new int[4]));
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
        MethodInfo createFactory = typeof(BigArray<byte>).GetMethod("CreateBigArrayFactory", BindingFlags.NonPublic | BindingFlags.Static, [typeof(int)])!;
        HashSet<int> factoryLengths = [];

        for (int elementSize = 1; elementSize <= 65535; elementSize++)
        {
            int chunkLength = 65535 / elementSize;

            Assert.True((long)chunkLength * elementSize <= 65535);

            if (factoryLengths.Add(chunkLength))
            {
                var factory = (Func<int, bool, bool, Array>)createFactory.Invoke(null, [chunkLength])!;
                Array storage = factory(1, false, false);

                Assert.True(storage.Length == 1);
                Assert.Equal(chunkLength, SizeOfArrayElement(storage.GetType().GetElementType()!));
            }
        }

        Assert.Equal(510, factoryLengths.Count);

        Type[] elementChunkTypes = typeof(BigArray<byte>).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(BigArray<byte>).Namespace)
            .Where(type => type.Name.StartsWith("ElementChunk", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(85, elementChunkTypes.Length);

        foreach (Type elementChunkType in elementChunkTypes)
        {
            string name = elementChunkType.Name;
            int arityMarker = name.IndexOf('`');
            int chunkLength = int.Parse(name["ElementChunk".Length..(arityMarker < 0 ? name.Length : arityMarker)]);

            Assert.True(chunkLength == 1 || IsPrime(chunkLength));
        }

        MethodInfo createReferenceFactory = typeof(BigArray<object>).GetMethod("CreateBigArrayFactory", BindingFlags.NonPublic | BindingFlags.Static, [typeof(int)])!;
        var referenceFactory = (Func<int, bool, bool, Array>)createReferenceFactory.Invoke(null, [65535 / Unsafe.SizeOf<object>()])!;

        Assert.Empty(referenceFactory(0, false, false));

        static bool IsPrime(int value)
        {
            for (int divisor = 2; divisor * divisor <= value; divisor++)
            {
                if (value % divisor == 0)
                {
                    return false;
                }
            }

            return value > 1;
        }
    }

    private static int SizeOfArrayElement(Type elementType)
    {
        MethodInfo sizeOf = typeof(HeziumMemoryApiTests).GetMethod(nameof(SizeOf), BindingFlags.NonPublic | BindingFlags.Static)!;
        return (int)sizeOf.MakeGenericMethod(elementType).Invoke(null, [])!;
    }

    private static int SizeOf<T>()
    {
        return Unsafe.SizeOf<T>();
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
