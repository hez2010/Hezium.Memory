# Hezium.Memory

[![NuGet Version](https://img.shields.io/nuget/v/Hezium.Memory)](https://www.nuget.org/packages/Hezium.Memory)

`BigArray<T>`, `BigMemory<T>`, `BigReadOnlyMemory<T>`, `BigSpan<T>`, and `BigReadOnlySpan<T>` for .NET code that wants the `Array`/`Memory`/`Span` programming model with `nint` lengths and indexes.

The standard `T[]` and `Span<T>` APIs are excellent until the array you want to model is larger than the largest single managed array. `Hezium.Memory` keeps the surface area familiar: allocate an owner, take a span-like view, slice it, search it, copy it, sort it, and pass references around without inventing a second indexing style.

```powershell
dotnet add package Hezium.Memory
```

```csharp
using Hezium.Memory;

nint length = 10_000_000_000;
BigArray<byte> buffer = new(length);

buffer[0] = 1;
buffer[5_000_000_000] = 2;
buffer[length - 1] = 3;

BigSpan<byte> window = buffer.AsBigSpan(5_000_000_000, 1024);

window[0] = 42;
Console.WriteLine(buffer[5_000_000_000]); // 42
```

`BigArray<T>` allocates on managed memory with GC support, so you can use it with reference types and it will be collected when no longer being used.

For explicit GC-style allocation, use `GC.AllocateBigArray<T>()` or `GC.AllocateUninitializedBigArray<T>()`. Both APIs accept the same `pinned` option as the built-in `GC` array allocation helpers.

```csharp
BigArray<byte> zeroed = GC.AllocateBigArray<byte>(length);
BigArray<byte> scratch = GC.AllocateUninitializedBigArray<byte>(length);
BigArray<byte> pinned = GC.AllocateBigArray<byte>(length, pinned: true);
```

## Why

Some workloads are naturally one-dimensional and very large: columnar data, precomputed lookup tables, native interop buffers, generated datasets, simulation state, and file-backed processing pipelines. The important part is not only "can I allocate a lot of elements", but "can I keep using the same mental model when I do?"

`Hezium.Memory` is built around that goal:

- `BigArray<T>` is the owning storage type.
- `BigMemory<T>` is the mutable storable view.
- `BigReadOnlyMemory<T>` is the read-only storable view.
- `BigSpan<T>` is the mutable stack-only view.
- `BigReadOnlySpan<T>` is the read-only stack-only view.
- Lengths, indexes, and offsets are `nint`.
- APIs intentionally resemble `Array`, `Memory<T>`, `ReadOnlyMemory<T>`, `Span<T>`, `ReadOnlySpan<T>`, and `MemoryMarshal`.

## BigArray

`BigArray<T>` is a one-dimensional, zero-based collection with a length that can exceed `Array.MaxLength`.

```csharp
using Hezium.Memory;

BigArray<int> array = new(10);

array.Fill(-1);
array[0] = 10;
array[array.Length - 1] = 90;

nint middle = array.IndexOf(-1);
bool hasNinety = array.Contains(90);

Console.WriteLine($"{array.Length}, {middle}, {hasNinety}");
```

The maximum length depends on the element size:

```csharp
nint byteCapacity = BigArray<byte>.MaxLength;
nint longCapacity = BigArray<long>.MaxLength;

Console.WriteLine(byteCapacity > Array.MaxLength);
Console.WriteLine(longCapacity > Array.MaxLength);
```

For normal-size arrays, `BigArray<T>` still behaves like an array-backed owner. Once the requested length is larger than `Array.MaxLength`, it switches to chunked storage while keeping one index space.

```csharp
BigArray<byte> buffer = new((nint)Array.MaxLength + 1024);

nint last = buffer.Length - 1;
buffer[last] = 255;

Console.WriteLine(buffer[last]);
```

## BigSpan

`BigSpan<T>` is a `ref struct` view over a contiguous region. It can be created from a `BigArray<T>`, a normal `Span<T>`, a reference, or a pointer.

```csharp
using Hezium.Memory;

BigArray<int> owner = new(1024);
BigSpan<int> span = owner.AsBigSpan();

span.Fill(1);

BigSpan<int> block = span.Slice(128, 256);
block.Clear();

owner[128] = 123;
Console.WriteLine(block[0]); // 123
```

The type is deliberately span-shaped: indexing returns by reference, slicing is cheap, `foreach` works, and `GetPinnableReference()` is available for pinning scenarios.

```csharp
Span<int> small = stackalloc int[] { 1, 2, 3, 4 };
BigSpan<int> big = small;

foreach (ref int item in big)
{
    item *= 2;
}
```

## BigReadOnlySpan

`BigReadOnlySpan<T>` is the read-only counterpart. `BigSpan<T>` converts to it implicitly, and normal `Span<T>`/`ReadOnlySpan<T>` values can be used as small read-only big spans.

```csharp
BigArray<int> data = new(5);
data.AsBigSpan().Fill(7);

BigReadOnlySpan<int> readOnly = data.AsBigSpan();

ref readonly int first = ref readOnly[0];
BigReadOnlySpan<int> tail = readOnly.Slice(1);

Console.WriteLine(first);
Console.WriteLine(tail.Length);
```

For text, take an explicit int-sized window before creating a string:

```csharp
BigReadOnlySpan<char> text = "hello".ToCharArray();
string middle = text.Slice(1, 3).ToSpan(0, 3).ToString();

Console.WriteLine(middle); // ell
```

## BigMemory

`BigMemory<T>` and `BigReadOnlyMemory<T>` mirror the storable `Memory<T>`/`ReadOnlyMemory<T>` shape with `nint` lengths. They can be sliced, copied, pinned, converted to arrays, and materialized as big spans when you need the hot by-ref path.

```csharp
using Hezium.Memory;

BigArray<int> owner = new(1024);
BigMemory<int> memory = owner.AsBigMemory(128, 256);

memory.Span.Fill(7);

BigMemory<int> tail = memory.Slice(128);
BigReadOnlyMemory<int> readOnly = memory;

Console.WriteLine(tail.Length);
Console.WriteLine(readOnly.Span[0]);
```

Normal arrays and array segments convert to big memory without copying:

```csharp
int[] small = [1, 2, 3, 4];

BigMemory<int> memory = small;
BigReadOnlyMemory<int> window = new ArraySegment<int>(small, 1, 2);

memory.Span[2] = 30;

Console.WriteLine(window.Span[1]); // 30
Console.WriteLine(small[2]); // 30
```

## Span-Like Operations

The extension methods follow the `Span<T>` vocabulary, but return `nint` where an index can be large.

```csharp
BigArray<int> numbers = new(8);
BigSpan<int> span = numbers.AsBigSpan();

for (nint i = 0; i < span.Length; i++)
{
    span[i] = (int)(i % 4);
}

nint firstTwo = span.IndexOf(2);
nint lastTwo = span.LastIndexOf(2);
bool startsWith = span.StartsWith(new[] { 0, 1 });

Console.WriteLine($"{firstTwo}, {lastTwo}, {startsWith}");
```

Copying works between big memory, big spans, normal spans, and `BigArray<T>`:

```csharp
BigArray<int> source = new(4);
source.AsBigSpan().Fill(9);

BigArray<int> destination = new(8);
source.CopyTo(destination, destinationIndex: 2);

int[] small = new int[4];
source.AsBigSpan().CopyTo(small);

BigMemory<int> memory = source.AsBigMemory();
BigMemory<int> memoryDestination = destination.AsBigMemory(2, 4);
memory.CopyTo(memoryDestination);
```

Search, trim, split, compare, and sort operations are available where the underlying .NET span APIs support them:

```csharp
BigSpan<int> values = new int[] { 0, 2, 1, 2, 0 };

BigSpan<int> trimmed = values.Trim(0);
nint marker = values.IndexOfAny(1, 9);

foreach (BigReadOnlySpan<int> segment in values.Split(2))
{
    Console.WriteLine(segment.Length);
}

values.Sort();
```

`SearchValues<T>` is supported for repeated searches over compatible element types:

```csharp
using System.Buffers;
using Hezium.Memory;

BigReadOnlySpan<byte> bytes = new byte[] { 1, 2, 3, 2, 1 };
SearchValues<byte> separators = SearchValues.Create((ReadOnlySpan<byte>)[2, 9]);

nint firstSeparator = bytes.IndexOfAny(separators);

foreach (BigReadOnlySpan<byte> segment in bytes.SplitAny(separators))
{
    Console.WriteLine(segment.Length);
}
```

## MemoryMarshal Helpers

`MemoryMarshal` extension members make it possible to create and inspect big spans from raw references.

```csharp
using System.Runtime.InteropServices;
using Hezium.Memory;

int value = 42;

BigSpan<int> span = MemoryMarshal.CreateBigSpan(ref value, length: 1);
ref int reference = ref MemoryMarshal.GetReference(span);

reference++;

BigReadOnlySpan<int> readOnly = span;
ref readonly int readOnlyReference = ref MemoryMarshal.GetReference(readOnly);

Console.WriteLine(readOnlyReference); // 43
```

## API Map

| Type | Purpose |
| --- | --- |
| `BigArray<T>` | Owning storage with `nint Length`, `MaxLength`, indexer, enumeration, `AsBigSpan`, `AsBigMemory`, and `AsSpan` for int-sized windows. |
| `BigMemory<T>` | Mutable storable view with `nint Length`, slicing, `Span`, copy/try-copy, pinning, `ToArray`, and `ToBigArray`. |
| `BigReadOnlyMemory<T>` | Read-only storable view with `nint Length`, slicing, `Span`, copy/try-copy, pinning, `ToArray`, and `ToBigArray`. |
| `BigSpan<T>` | Mutable `ref struct` view with `nint Length`, slicing, by-ref indexing, pinning, enumeration, copy/search/trim/split/sort helpers. |
| `BigReadOnlySpan<T>` | Read-only `ref struct` view with slicing, by-readonly-ref indexing, pinning, enumeration, copy/search/trim/split helpers. |
| `MemoryMarshal` extensions | `CreateBigSpan`, `GetReference(BigSpan<T>)`, and `GetReference(BigReadOnlySpan<T>)`. |
| `GC` extensions | `AllocateBigArray<T>` and `AllocateUninitializedBigArray<T>` with optional pinned storage. |

## Requirements

- .NET 10 or later

## Notes

- `BigMemory<T>` and `BigReadOnlyMemory<T>` are regular structs that can be stored; their `Span` properties produce stack-only big span views.
- `BigSpan<T>` and `BigReadOnlySpan<T>` are `ref struct` types, so they follow the same stack-only lifetime rules as `Span<T>`.
- `ToArray()` requires the span or array to fit into a single `T[]`; use `ToBigArray()` when the result may exceed `Array.MaxLength`.
- `BigArray<T>.MaxLength` is element-size dependent.
- Value types larger than 65535 bytes are rejected.

## Benchmarks

Benchmark agsinst `BigArray<T>` and `T[][]` (jagged array). Chunk size for large arrays is `Array.MaxLength` for both implementations.

Environment:

- CPU: AMD EPYC 9V74 with 48 cores
- OS: Ubuntu 24.04.4 LTS (GNU/Linux 6.17.0-1018-azure x86_64)
- Memory: 192 GB
- .NET: 10.0.109

### AllocationBenchmarks

| Method      | Job        | Server | Length     | Mean         | Error        | StdDev       | Gen0     | Gen1     | Gen2     | Allocated   |
|------------ |----------- |------- |----------- |-------------:|-------------:|-------------:|---------:|---------:|---------:|------------:|
| JaggedArray | Job-MSAXQN | False  | 1048576    |     107.2 us |      0.66 us |      0.59 us | 330.9326 | 330.5664 | 330.5664 |        4 MB |
| BigArray    | Job-MSAXQN | False  | 1048576    |     107.9 us |      1.07 us |      1.00 us | 333.2520 | 333.0078 | 333.0078 |        4 MB |
| JaggedArray | Job-WGBUQW | True   | 1048576    |     134.5 us |      3.04 us |      4.56 us |   2.9297 |   2.9297 |   2.9297 |        4 MB |
| BigArray    | Job-WGBUQW | True   | 1048576    |     138.6 us |      2.69 us |      4.03 us |   3.1738 |   3.1738 |   3.1738 |        4 MB |
| JaggedArray | Job-MSAXQN | False  | 4294967296 | 310,164.6 us | 35,804.08 us | 53,589.86 us | 437.5000 | 437.5000 | 437.5000 |    16384 MB |
| BigArray    | Job-MSAXQN | False  | 4294967296 | 252,474.1 us |    111.41 us |     86.98 us | 500.0000 | 500.0000 | 500.0000 | 16384.06 MB |
| JaggedArray | Job-WGBUQW | True   | 4294967296 |     642.1 us |    330.94 us |    495.33 us |  62.5000 |  62.5000 |  62.5000 |    16384 MB |
| BigArray    | Job-WGBUQW | True   | 4294967296 |     542.0 us |    285.15 us |    426.80 us |        - |        - |        - | 16384.06 MB |

### IndexedAccessBenchmarks

| Method              | Job        | Server | Length     | Mean      | Error     | StdDev    | Allocated |
|-------------------- |----------- |------- |----------- |----------:|----------:|----------:|----------:|
| JaggedRandomLoad    | Job-MSAXQN | False  | 1048576    | 16.061 us | 0.0061 us | 0.0057 us |         - |
| BigArrayRandomLoad  | Job-MSAXQN | False  | 1048576    |  7.795 us | 0.0218 us | 0.0182 us |         - |
| JaggedRandomLoad    | Job-WGBUQW | True   | 1048576    | 15.591 us | 0.0453 us | 0.0401 us |         - |
| BigArrayRandomLoad  | Job-WGBUQW | True   | 1048576    |  8.441 us | 0.0291 us | 0.0258 us |         - |
| JaggedRandomLoad    | Job-MSAXQN | False  | 4294967296 | 27.433 us | 0.0585 us | 0.0547 us |         - |
| BigArrayRandomLoad  | Job-MSAXQN | False  | 4294967296 | 17.162 us | 0.0143 us | 0.0134 us |         - |
| JaggedRandomLoad    | Job-WGBUQW | True   | 4294967296 | 26.653 us | 0.0382 us | 0.0339 us |         - |
| BigArrayRandomLoad  | Job-WGBUQW | True   | 4294967296 | 16.965 us | 0.1833 us | 0.1715 us |         - |
| JaggedRandomStore   | Job-MSAXQN | False  | 1048576    | 30.397 us | 0.0288 us | 0.0256 us |         - |
| BigArrayRandomStore | Job-MSAXQN | False  | 1048576    | 17.183 us | 0.0072 us | 0.0067 us |         - |
| JaggedRandomStore   | Job-WGBUQW | True   | 1048576    | 39.376 us | 0.4035 us | 0.3774 us |         - |
| BigArrayRandomStore | Job-WGBUQW | True   | 1048576    | 14.616 us | 0.0087 us | 0.0082 us |         - |
| JaggedRandomStore   | Job-MSAXQN | False  | 4294967296 | 27.179 us | 0.0432 us | 0.0404 us |         - |
| BigArrayRandomStore | Job-MSAXQN | False  | 4294967296 | 19.859 us | 0.0291 us | 0.0272 us |         - |
| JaggedRandomStore   | Job-WGBUQW | True   | 4294967296 | 27.009 us | 0.0330 us | 0.0309 us |         - |
| BigArrayRandomStore | Job-WGBUQW | True   | 4294967296 | 19.891 us | 0.0330 us | 0.0309 us |         - |

### SearchValuesBenchmarks

| Method                 | Job        | Server | Length     | Mean               | Error             | StdDev            | Allocated |
|----------------------- |----------- |------- |----------- |-------------------:|------------------:|------------------:|----------:|
| JaggedIndexOfAny       | Job-MSAXQN | False  | 1048576    |      35,053.956 ns |         6.4725 ns |         5.7377 ns |         - |
| BigArrayIndexOfAny     | Job-MSAXQN | False  | 1048576    |      33,964.603 ns |       426.6438 ns |       399.0828 ns |         - |
| JaggedIndexOfAny       | Job-WGBUQW | True   | 1048576    |      34,118.354 ns |         5.2324 ns |         4.3693 ns |         - |
| BigArrayIndexOfAny     | Job-WGBUQW | True   | 1048576    |      34,067.148 ns |        52.0565 ns |        48.6937 ns |         - |
| JaggedIndexOfAny       | Job-MSAXQN | False  | 4294967296 | 149,301,032.083 ns |    81,675.7728 ns |    76,399.5686 ns |         - |
| BigArrayIndexOfAny     | Job-MSAXQN | False  | 4294967296 | 146,967,150.295 ns | 3,405,507.4754 ns | 4,884,077.4653 ns |         - |
| JaggedIndexOfAny       | Job-WGBUQW | True   | 4294967296 | 149,417,332.327 ns |   179,863.5718 ns |   150,194.2286 ns |         - |
| BigArrayIndexOfAny     | Job-WGBUQW | True   | 4294967296 | 149,669,044.333 ns |   128,632.1971 ns |   120,322.6370 ns |         - |
| JaggedLastIndexOfAny   | Job-MSAXQN | False  | 1048576    |           4.095 ns |         0.0053 ns |         0.0050 ns |         - |
| BigArrayLastIndexOfAny | Job-MSAXQN | False  | 1048576    |           3.621 ns |         0.0044 ns |         0.0039 ns |         - |
| JaggedLastIndexOfAny   | Job-WGBUQW | True   | 1048576    |           4.128 ns |         0.0051 ns |         0.0048 ns |         - |
| BigArrayLastIndexOfAny | Job-WGBUQW | True   | 1048576    |           3.613 ns |         0.0040 ns |         0.0035 ns |         - |
| JaggedLastIndexOfAny   | Job-MSAXQN | False  | 4294967296 |           3.757 ns |         0.0067 ns |         0.0060 ns |         - |
| BigArrayLastIndexOfAny | Job-MSAXQN | False  | 4294967296 |           5.475 ns |         0.0023 ns |         0.0021 ns |         - |
| JaggedLastIndexOfAny   | Job-WGBUQW | True   | 4294967296 |           4.327 ns |         0.0069 ns |         0.0058 ns |         - |
| BigArrayLastIndexOfAny | Job-WGBUQW | True   | 4294967296 |           7.560 ns |         0.0271 ns |         0.0253 ns |         - | 

### SpanAlgorithmBenchmarks

| Method                | Job        | Server | Length     | Mean                | Error             | StdDev             | Median            | Allocated |
|---------------------- |----------- |------- |----------- |--------------------:|------------------:|-------------------:|------------------:|----------:|
| JaggedBinarySearch    | Job-MSAXQN | False  | 1048576    |            50.33 ns |          0.023 ns |           0.021 ns |          50.33 ns |         - |
| BigArrayBinarySearch  | Job-MSAXQN | False  | 1048576    |            18.63 ns |          0.002 ns |           0.002 ns |          18.63 ns |         - |
| JaggedBinarySearch    | Job-WGBUQW | True   | 1048576    |            52.33 ns |          0.050 ns |           0.046 ns |          52.33 ns |         - |
| BigArrayBinarySearch  | Job-WGBUQW | True   | 1048576    |            18.62 ns |          0.007 ns |           0.006 ns |          18.62 ns |         - |
| JaggedBinarySearch    | Job-MSAXQN | False  | 4294967296 |            95.37 ns |          0.205 ns |           0.192 ns |          95.40 ns |         - |
| BigArrayBinarySearch  | Job-MSAXQN | False  | 4294967296 |            87.15 ns |          0.019 ns |           0.018 ns |          87.15 ns |         - |
| JaggedBinarySearch    | Job-WGBUQW | True   | 4294967296 |            93.97 ns |          0.114 ns |           0.106 ns |          93.99 ns |         - |
| BigArrayBinarySearch  | Job-WGBUQW | True   | 4294967296 |            87.40 ns |          1.772 ns |           1.819 ns |          87.39 ns |         - |
| JaggedCopyTo          | Job-MSAXQN | False  | 1048576    |        98,646.13 ns |         31.179 ns |          29.164 ns |      98,650.36 ns |         - |
| BigArrayCopyTo        | Job-MSAXQN | False  | 1048576    |        98,171.28 ns |        224.522 ns |         210.018 ns |      98,074.42 ns |         - |
| JaggedCopyTo          | Job-WGBUQW | True   | 1048576    |        98,473.88 ns |         55.944 ns |          52.330 ns |      98,482.03 ns |         - |
| BigArrayCopyTo        | Job-WGBUQW | True   | 1048576    |        98,083.98 ns |         15.461 ns |          13.706 ns |      98,086.00 ns |         - |
| JaggedCopyTo          | Job-MSAXQN | False  | 4294967296 |   878,563,703.87 ns |  3,154,969.517 ns |   2,951,160.445 ns | 879,360,475.00 ns |         - |
| BigArrayCopyTo        | Job-MSAXQN | False  | 4294967296 |   883,238,395.62 ns |  1,118,339.068 ns |     933,863.772 ns | 883,148,056.00 ns |         - |
| JaggedCopyTo          | Job-WGBUQW | True   | 4294967296 |   871,257,708.47 ns |  4,085,053.488 ns |   3,821,161.569 ns | 873,508,862.00 ns |         - |
| BigArrayCopyTo        | Job-WGBUQW | True   | 4294967296 |   879,123,890.80 ns |  5,195,745.994 ns |   4,860,104.029 ns | 877,022,518.00 ns |         - |
| JaggedFill            | Job-MSAXQN | False  | 1048576    |        58,406.84 ns |         27.959 ns |          26.153 ns |      58,403.43 ns |         - |
| BigArrayFill          | Job-MSAXQN | False  | 1048576    |        45,774.94 ns |         13.581 ns |          11.341 ns |      45,773.56 ns |         - |
| JaggedFill            | Job-WGBUQW | True   | 1048576    |        58,419.69 ns |        260.430 ns |         243.607 ns |      58,298.38 ns |         - |
| BigArrayFill          | Job-WGBUQW | True   | 1048576    |        45,821.16 ns |         51.402 ns |          45.566 ns |      45,805.17 ns |         - |
| JaggedFill            | Job-MSAXQN | False  | 4294967296 |   644,735,516.31 ns |  1,667,287.227 ns |   1,392,260.347 ns | 644,785,781.00 ns |         - |
| BigArrayFill          | Job-MSAXQN | False  | 4294967296 |   638,785,676.23 ns |  1,216,150.005 ns |   1,015,540.334 ns | 638,407,536.00 ns |         - |
| JaggedFill            | Job-WGBUQW | True   | 4294967296 |   644,025,288.92 ns |  2,234,161.872 ns |   1,865,626.349 ns | 645,052,053.00 ns |         - |
| BigArrayFill          | Job-WGBUQW | True   | 4294967296 |   651,020,320.00 ns | 12,279,121.406 ns |  12,059,739.093 ns | 648,530,244.50 ns |         - |
| JaggedSequenceEqual   | Job-MSAXQN | False  | 1048576    |       101,852.53 ns |         35.331 ns |          33.049 ns |     101,856.54 ns |         - |
| BigArraySequenceEqual | Job-MSAXQN | False  | 1048576    |        94,793.59 ns |         44.223 ns |          41.366 ns |      94,787.66 ns |         - |
| JaggedSequenceEqual   | Job-WGBUQW | True   | 1048576    |       102,010.81 ns |        197.603 ns |         184.838 ns |     102,065.77 ns |         - |
| BigArraySequenceEqual | Job-WGBUQW | True   | 1048576    |        94,676.34 ns |        131.274 ns |         122.794 ns |      94,663.52 ns |         - |
| JaggedSequenceEqual   | Job-MSAXQN | False  | 4294967296 |   969,067,769.62 ns | 64,933,219.624 ns |  95,178,279.239 ns | 916,838,685.00 ns |         - |
| BigArraySequenceEqual | Job-MSAXQN | False  | 4294967296 |   913,358,414.62 ns | 13,390,304.429 ns |  13,151,069.401 ns | 906,787,846.50 ns |         - |
| JaggedSequenceEqual   | Job-WGBUQW | True   | 4294967296 | 1,016,012,640.73 ns | 81,407,287.188 ns | 121,846,604.367 ns | 951,406,969.50 ns |         - |
| BigArraySequenceEqual | Job-WGBUQW | True   | 4294967296 |   978,172,288.14 ns | 56,704,153.943 ns |  83,116,220.468 ns | 932,240,221.00 ns |         - |

## License

MIT License.
