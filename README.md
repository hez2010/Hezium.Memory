# Hezium.Memory

`BigArray<T>`, `BigSpan<T>`, and `BigReadOnlySpan<T>` for .NET code that wants the `Array`/`Span` programming model with `nint` lengths and indexes.

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

## Why

Some workloads are naturally one-dimensional and very large: columnar data, precomputed lookup tables, native interop buffers, generated datasets, simulation state, and file-backed processing pipelines. The important part is not only "can I allocate a lot of elements", but "can I keep using the same mental model when I do?"

`Hezium.Memory` is built around that goal:

- `BigArray<T>` is the owning storage type.
- `BigSpan<T>` is the mutable stack-only view.
- `BigReadOnlySpan<T>` is the read-only stack-only view.
- Lengths, indexes, and offsets are `nint`.
- APIs intentionally resemble `Array`, `Span<T>`, `ReadOnlySpan<T>`, and `MemoryMarshal`.

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

Copying works between big spans, normal spans, and `BigArray<T>`:

```csharp
BigArray<int> source = new(4);
source.AsBigSpan().Fill(9);

BigArray<int> destination = new(8);
source.CopyTo(destination, destinationIndex: 2);

int[] small = new int[4];
source.AsBigSpan().CopyTo(small);
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
| `BigArray<T>` | Owning storage with `nint Length`, `MaxLength`, indexer, enumeration, `AsBigSpan`, and `AsSpan` for int-sized windows. |
| `BigSpan<T>` | Mutable `ref struct` view with `nint Length`, slicing, by-ref indexing, pinning, enumeration, copy/search/trim/split/sort helpers. |
| `BigReadOnlySpan<T>` | Read-only `ref struct` view with slicing, by-readonly-ref indexing, pinning, enumeration, copy/search/trim/split helpers. |
| `MemoryMarshal` extensions | `CreateBigSpan`, `GetReference(BigSpan<T>)`, and `GetReference(BigReadOnlySpan<T>)`. |

## Requirements

- .NET 10 or later

## Notes

- `BigSpan<T>` and `BigReadOnlySpan<T>` are `ref struct` types, so they follow the same stack-only lifetime rules as `Span<T>`.
- `ToArray()` requires the span or array to fit into a single `T[]`; use `ToBigArray()` when the result may exceed `Array.MaxLength`.
- `BigArray<T>.MaxLength` is element-size dependent.
- Value types larger than 65535 bytes are rejected.

## License

MIT License.
