# BigArray

`BigArray` provides array-like storage and span-like views that use `nint` lengths and indexes, allowing logical collections larger than `Array.MaxLength`.

## Requirements

- .NET 10 or above

## Quick Start

```csharp
using Hezium.Collections;

var values = new BigArray<long>(10);

values[0] = 42;
values[9] = 100;

for (nint i = 0; i < values.Length; i++)
{
    values[i] = i;
}

Console.WriteLine(values[9]);
```

Use `BigArray<T>.MaxLength` to check the largest supported logical length for an element type:

```csharp
nint maxLongs = BigArray<long>.MaxLength;
var values = new BigArray<long>(maxLongs);
```

Value-type elements whose size is larger than 65535 bytes are not supported.

## BigSpan

`BigSpan<T>` is a `ref struct` view over a contiguous logical range of elements. You can create one from a `BigArray<T>`:

```csharp
BigArray<int> array = new(1024);
BigSpan<int> span = array.AsBigSpan();

span[0] = 1;
span[1] = 2;

BigSpan<int> tail = span.Slice(1);
BigSpan<int> firstTen = span.Slice(0, 10);
```

## BigReadOnlySpan

`BigReadOnlySpan<T>` is the read-only counterpart to `BigSpan<T>`:

```csharp
BigArray<int> array = new(1024);
BigReadOnlySpan<int> span = array.AsBigSpan();

ref readonly int first = ref span[0];
BigReadOnlySpan<int> rest = span.Slice(1);
```

## MemoryMarshal Helpers

The library adds `MemoryMarshal` extension members for creating and inspecting big spans:

```csharp
using System.Runtime.InteropServices;
using Hezium.Collections;

int first = 123;

BigSpan<int> span = MemoryMarshal.CreateBigSpan(ref first, 1);
ref int reference = ref MemoryMarshal.GetReference(span);
```

For read-only spans:

```csharp
BigReadOnlySpan<int> readOnlySpan = span;
ref readonly int reference = ref MemoryMarshal.GetReference(readOnlySpan);
```

## Span Operations

`BigSpan<T>` has extension methods for common sequence operations:

```csharp
BigArray<int> source = new(1024);
BigArray<int> destination = new(1024);

BigSpan<int> sourceSpan = source.AsBigSpan();
BigSpan<int> destinationSpan = destination.AsBigSpan();

sourceSpan.CopyTo(destinationSpan);

bool equal = sourceSpan.SequenceEqual(destinationSpan);
bool contains42 = sourceSpan.Contains(42);
int comparison = sourceSpan.SequenceCompareTo(destinationSpan);
```

These operations process the data in regular `Span<T>` sized segments internally.
