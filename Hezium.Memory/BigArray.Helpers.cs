using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Hezium.Memory;

public sealed partial class BigArray<T>
{
    private const int MaxChunkByteLength = 65535;

    internal readonly struct BigArrayFactory
    {
        private readonly Func<int, bool, bool, Array> _allocate;

        internal BigArrayFactory(int elementLength, int elementByteLength, Func<int, bool, bool, Array> allocate)
        {
            ElementLength = elementLength;
            ElementByteLength = elementByteLength;
            _allocate = allocate;
        }

        internal int ElementLength { get; }

        internal int ElementByteLength { get; }

        internal Array Allocate(int chunks, bool pinned, bool uninitialized)
        {
            return _allocate(chunks, pinned, uninitialized);
        }
    }

    // Exhaustive for every value produced by 65535 / sizeof(T). Composite
    // lengths are represented by recursively nesting the prime-factor chunk
    // structs. Keep each nesting step behind a lambda so unselected chunk
    // types stay lazy and cannot fail type loading before they are used.
    internal static BigArrayFactory CreateBigArrayFactory(int chunkLength)
    {
        if ((uint)(chunkLength - 1) >= MaxChunkByteLength || MaxChunkByteLength / (MaxChunkByteLength / chunkLength) != chunkLength)
        {
            throw new UnreachableException();
        }

        return chunkLength == 1
            ? CreateBigArrayFactoryCore<ElementChunk1<T>>(1)
            : CreateBigArrayFactoryCore<T>(chunkLength);
    }

    private static BigArrayFactory CreateBigArrayFactoryCore<TElement>(int remainingElementCount)
    {
        if (remainingElementCount == 1)
        {
            int elementByteLength = Unsafe.SizeOf<TElement>();
            return new BigArrayFactory(elementByteLength / Unsafe.SizeOf<T>(), elementByteLength, AllocateArray<TElement>);
        }

        int factor = GetPrimeFactor(remainingElementCount);
        return GetBigArrayFactoryComposer<TElement>(factor)(remainingElementCount / factor);
    }

    private static Func<int, BigArrayFactory> GetBigArrayFactoryComposer<TElement>(int factor) => factor switch
    {
        2 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk2<TElement>>(remainingElementCount),
        3 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk3<TElement>>(remainingElementCount),
        5 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk5<TElement>>(remainingElementCount),
        7 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk7<TElement>>(remainingElementCount),
        11 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk11<TElement>>(remainingElementCount),
        13 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk13<TElement>>(remainingElementCount),
        17 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk17<TElement>>(remainingElementCount),
        19 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk19<TElement>>(remainingElementCount),
        23 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk23<TElement>>(remainingElementCount),
        29 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk29<TElement>>(remainingElementCount),
        31 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk31<TElement>>(remainingElementCount),
        37 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk37<TElement>>(remainingElementCount),
        41 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk41<TElement>>(remainingElementCount),
        43 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk43<TElement>>(remainingElementCount),
        47 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk47<TElement>>(remainingElementCount),
        53 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk53<TElement>>(remainingElementCount),
        59 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk59<TElement>>(remainingElementCount),
        61 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk61<TElement>>(remainingElementCount),
        67 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk67<TElement>>(remainingElementCount),
        71 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk71<TElement>>(remainingElementCount),
        73 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk73<TElement>>(remainingElementCount),
        79 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk79<TElement>>(remainingElementCount),
        83 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk83<TElement>>(remainingElementCount),
        89 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk89<TElement>>(remainingElementCount),
        97 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk97<TElement>>(remainingElementCount),
        101 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk101<TElement>>(remainingElementCount),
        103 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk103<TElement>>(remainingElementCount),
        107 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk107<TElement>>(remainingElementCount),
        109 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk109<TElement>>(remainingElementCount),
        113 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk113<TElement>>(remainingElementCount),
        127 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk127<TElement>>(remainingElementCount),
        131 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk131<TElement>>(remainingElementCount),
        137 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk137<TElement>>(remainingElementCount),
        139 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk139<TElement>>(remainingElementCount),
        149 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk149<TElement>>(remainingElementCount),
        151 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk151<TElement>>(remainingElementCount),
        157 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk157<TElement>>(remainingElementCount),
        163 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk163<TElement>>(remainingElementCount),
        167 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk167<TElement>>(remainingElementCount),
        173 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk173<TElement>>(remainingElementCount),
        179 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk179<TElement>>(remainingElementCount),
        181 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk181<TElement>>(remainingElementCount),
        191 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk191<TElement>>(remainingElementCount),
        193 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk193<TElement>>(remainingElementCount),
        197 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk197<TElement>>(remainingElementCount),
        199 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk199<TElement>>(remainingElementCount),
        211 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk211<TElement>>(remainingElementCount),
        223 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk223<TElement>>(remainingElementCount),
        227 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk227<TElement>>(remainingElementCount),
        229 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk229<TElement>>(remainingElementCount),
        233 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk233<TElement>>(remainingElementCount),
        239 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk239<TElement>>(remainingElementCount),
        241 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk241<TElement>>(remainingElementCount),
        251 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk251<TElement>>(remainingElementCount),
        257 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk257<TElement>>(remainingElementCount),
        263 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk263<TElement>>(remainingElementCount),
        269 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk269<TElement>>(remainingElementCount),
        271 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk271<TElement>>(remainingElementCount),
        277 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk277<TElement>>(remainingElementCount),
        281 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk281<TElement>>(remainingElementCount),
        283 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk283<TElement>>(remainingElementCount),
        293 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk293<TElement>>(remainingElementCount),
        307 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk307<TElement>>(remainingElementCount),
        313 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk313<TElement>>(remainingElementCount),
        337 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk337<TElement>>(remainingElementCount),
        383 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk383<TElement>>(remainingElementCount),
        397 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk397<TElement>>(remainingElementCount),
        409 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk409<TElement>>(remainingElementCount),
        431 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk431<TElement>>(remainingElementCount),
        439 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk439<TElement>>(remainingElementCount),
        461 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk461<TElement>>(remainingElementCount),
        541 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk541<TElement>>(remainingElementCount),
        569 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk569<TElement>>(remainingElementCount),
        601 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk601<TElement>>(remainingElementCount),
        661 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk661<TElement>>(remainingElementCount),
        809 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk809<TElement>>(remainingElementCount),
        829 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk829<TElement>>(remainingElementCount),
        1129 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk1129<TElement>>(remainingElementCount),
        1213 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk1213<TElement>>(remainingElementCount),
        1489 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk1489<TElement>>(remainingElementCount),
        2621 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk2621<TElement>>(remainingElementCount),
        3449 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk3449<TElement>>(remainingElementCount),
        6553 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk6553<TElement>>(remainingElementCount),
        8191 => static remainingElementCount => CreateBigArrayFactoryCore<ElementChunk8191<TElement>>(remainingElementCount),
        _ => throw new UnreachableException()
    };

    private static int GetPrimeFactor(int value)
    {
        return BigArrayChunkFactors.GetPrimeFactor(value);
    }

    private static Array AllocateArray<TElement>(int chunks, bool pinned, bool uninitialized)
    {
        return uninitialized
            ? GC.AllocateUninitializedArray<TElement>(chunks, pinned)
            : GC.AllocateArray<TElement>(chunks, pinned);
    }

    private static int GetChunkLength()
    {
        return MaxChunkByteLength / Unsafe.SizeOf<T>();
    }

    private static Array CreateBigArraySlow(nint length, bool pinned = false, bool uninitialized = false)
    {
        int chunkLength = GetChunkLength();
        int chunks = (int)((length / chunkLength) + (length % chunkLength == 0 ? 0 : 1));

        return CreateBigArrayFactory(chunkLength).Allocate(chunks, pinned, uninitialized);
    }
}

file static class BigArrayChunkFactors
{
    private const int MaxChunkByteLength = 65535;

    private static readonly ushort[] s_smallestPrimeFactors = CreateSmallestPrimeFactors();

    internal static int GetPrimeFactor(int value)
    {
        Debug.Assert((uint)value <= MaxChunkByteLength);
        return s_smallestPrimeFactors[value];
    }

    private static ushort[] CreateSmallestPrimeFactors()
    {
        ushort[] factors = new ushort[MaxChunkByteLength + 1];
        factors[1] = 1;

        for (int value = 2; value < factors.Length; value++)
        {
            if (factors[value] != 0)
            {
                continue;
            }

            factors[value] = (ushort)value;

            if (value > MaxChunkByteLength / value)
            {
                continue;
            }

            for (int multiple = value * value; multiple <= MaxChunkByteLength; multiple += value)
            {
                if (factors[multiple] == 0)
                {
                    factors[multiple] = (ushort)value;
                }
            }
        }

        return factors;
    }
}
