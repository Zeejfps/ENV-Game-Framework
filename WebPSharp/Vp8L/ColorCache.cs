namespace WebPSharp.Vp8L;

/// <summary>
/// The VP8L color cache: a small hash-indexed table of recently emitted ARGB pixels. During
/// decoding a "cache index" symbol reproduces a previously seen color in one lookup; every pixel
/// produced (whether literal, back-reference, or cache hit) is inserted so encoder and decoder
/// stay in lock-step.
/// </summary>
internal sealed class ColorCache
{
    private const uint HashMultiplier = 0x1E35A7BDu;

    private readonly uint[] _colors;
    private readonly int _shift;

    /// <summary>Creates a color cache addressed by <paramref name="bits"/> hash bits.</summary>
    /// <param name="bits">The number of index bits, 1..11 (cache size <c>2^bits</c>).</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="bits"/> is outside 1..11.</exception>
    public ColorCache(int bits)
    {
        if (bits is < 1 or > 11)
            throw new ArgumentOutOfRangeException(nameof(bits), bits, "Color cache bits must be between 1 and 11.");
        _colors = new uint[1 << bits];
        _shift = 32 - bits;
    }

    /// <summary>The number of entries in the cache.</summary>
    public int Size => _colors.Length;

    /// <summary>Computes the cache index for a color.</summary>
    /// <param name="argb">The color in 0xAARRGGBB order.</param>
    /// <returns>The hash index into the cache.</returns>
    public int GetIndex(uint argb) => (int)((HashMultiplier * argb) >> _shift);

    /// <summary>Inserts a color at its hashed slot, overwriting any previous occupant.</summary>
    /// <param name="argb">The color in 0xAARRGGBB order.</param>
    public void Insert(uint argb) => _colors[GetIndex(argb)] = argb;

    /// <summary>Returns the color stored at <paramref name="index"/>.</summary>
    /// <param name="index">A cache index, 0..<see cref="Size"/>-1.</param>
    /// <returns>The stored color.</returns>
    public uint Lookup(int index) => _colors[index];
}
