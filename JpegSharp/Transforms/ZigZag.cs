namespace JpegSharp.Transforms;

/// <summary>
/// Provides the JPEG zig-zag coefficient ordering (ITU-T T.81 Annex A, Figure A.6)
/// and helpers to convert an 8x8 block between natural raster order and zig-zag order.
/// </summary>
/// <remarks>
/// Entropy coding walks the 64 quantized coefficients of a block in zig-zag order so that
/// low-frequency (typically non-zero) coefficients are grouped ahead of high-frequency
/// (typically zero) coefficients, maximizing run-lengths of zeros.
/// </remarks>
internal static class ZigZag
{
    /// <summary>The number of coefficients in an 8x8 DCT block.</summary>
    public const int BlockSize = 64;

    private static readonly int[] OrderTable =
    [
        0, 1, 8, 16, 9, 2, 3, 10,
        17, 24, 32, 25, 18, 11, 4, 5,
        12, 19, 26, 33, 40, 48, 41, 34,
        27, 20, 13, 6, 7, 14, 21, 28,
        35, 42, 49, 56, 57, 50, 43, 36,
        29, 22, 15, 23, 30, 37, 44, 51,
        58, 59, 52, 45, 38, 31, 39, 46,
        53, 60, 61, 54, 47, 55, 62, 63
    ];

    private static readonly int[] InverseTable = BuildInverse(OrderTable);

    /// <summary>
    /// Maps a zig-zag position (0..63) to the natural raster index (0..63) of the coefficient
    /// stored there. <c>Order[0]</c> is always the DC coefficient at natural index 0.
    /// </summary>
    public static ReadOnlySpan<int> Order => OrderTable;

    /// <summary>
    /// Maps a natural raster index (0..63) to the zig-zag position (0..63) at which that
    /// coefficient is coded. This is the inverse permutation of <see cref="Order"/>.
    /// </summary>
    public static ReadOnlySpan<int> InverseOrder => InverseTable;

    /// <summary>
    /// Reorders a block from natural raster order into zig-zag order.
    /// </summary>
    /// <param name="natural">Source block of 64 coefficients in natural raster order.</param>
    /// <param name="zigzag">Destination block of 64 coefficients in zig-zag order.</param>
    /// <exception cref="ArgumentException">A span is not exactly 64 elements long.</exception>
    public static void FromNatural(ReadOnlySpan<short> natural, Span<short> zigzag)
    {
        EnsureBlock(natural, nameof(natural));
        EnsureBlock(zigzag, nameof(zigzag));

        ref var order = ref OrderTable[0];
        for (var k = 0; k < BlockSize; k++)
            zigzag[k] = natural[System.Runtime.CompilerServices.Unsafe.Add(ref order, k)];
    }

    /// <summary>
    /// Reorders a block from zig-zag order back into natural raster order.
    /// </summary>
    /// <param name="zigzag">Source block of 64 coefficients in zig-zag order.</param>
    /// <param name="natural">Destination block of 64 coefficients in natural raster order.</param>
    /// <exception cref="ArgumentException">A span is not exactly 64 elements long.</exception>
    public static void ToNatural(ReadOnlySpan<short> zigzag, Span<short> natural)
    {
        EnsureBlock(zigzag, nameof(zigzag));
        EnsureBlock(natural, nameof(natural));

        ref var order = ref OrderTable[0];
        for (var k = 0; k < BlockSize; k++)
            natural[System.Runtime.CompilerServices.Unsafe.Add(ref order, k)] = zigzag[k];
    }

    private static int[] BuildInverse(int[] order)
    {
        var inverse = new int[BlockSize];
        for (var k = 0; k < BlockSize; k++)
            inverse[order[k]] = k;
        return inverse;
    }

    private static void EnsureBlock<T>(ReadOnlySpan<T> span, string name)
    {
        if (span.Length != BlockSize)
            throw new ArgumentException($"Block must contain exactly {BlockSize} coefficients.", name);
    }
}
