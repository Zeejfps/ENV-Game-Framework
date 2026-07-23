using WebPSharp.Vp8L.Transforms;

namespace WebPSharp.Tests;

public class ColorIndexingTransformTests
{
    [Theory]
    [InlineData(2, 3)]
    [InlineData(3, 2)]
    [InlineData(4, 2)]
    [InlineData(5, 1)]
    [InlineData(16, 1)]
    [InlineData(17, 0)]
    [InlineData(256, 0)]
    public void BitsForColorCount_MatchesSpec(int numColors, int expectedBits)
    {
        Assert.Equal(expectedBits, ColorIndexingTransform.BitsForColorCount(numColors));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(16)]
    [InlineData(200)]
    public void BundleThenInverse_ReproducesPaletteColors(int numColors)
    {
        const int width = 37, height = 11;
        var rng = new Random(numColors);
        var bits = ColorIndexingTransform.BitsForColorCount(numColors);
        var finalColors = 1 << (8 >> bits);

        // Random palette padded to the addressable size.
        var palette = new uint[finalColors];
        for (var i = 0; i < numColors; i++)
            palette[i] = (uint)rng.NextInt64(0, 1L << 32);

        // Random indices in range.
        var indices = new byte[width * height];
        for (var i = 0; i < indices.Length; i++)
            indices[i] = (byte)rng.Next(numColors);

        var bundledWidth = (width + (1 << bits) - 1) >> bits;
        var bundled = ColorIndexingTransform.Bundle(indices, width, height, bits);
        Assert.Equal(bundledWidth * height, bundled.Length);

        var restored = ColorIndexingTransform.Inverse(bundled, bundledWidth, width, height, palette, bits);

        for (var i = 0; i < indices.Length; i++)
            Assert.Equal(palette[indices[i]], restored[i]);
    }

    [Fact]
    public void Inverse_IndexBeyondPopulatedPalette_MapsToZero()
    {
        // The palette is always allocated to the full addressable range (1 << (8 >> bits)); entries
        // past the actual color count are zero padding. A bundled index that lands on that padding
        // must safely return 0x00000000 (transparent black) rather than crashing. Here bits=0 means
        // one index per pixel taken straight from the green byte, so index 200 addresses padding.
        var palette = new uint[256];
        palette[0] = 0xFF112233u;
        palette[1] = 0xFF445566u;
        palette[2] = 0xFF778899u;
        // palette[3..255] remain 0 (padding).

        // bits=0: Inverse reads the index from (pixel >> 8) & 0xFF. Green bytes: 1, 200, 2.
        var bundled = new[] { 1u << 8, 200u << 8, 2u << 8 };
        var restored = ColorIndexingTransform.Inverse(bundled, bundledWidth: 3, fullWidth: 3, height: 1, palette, bits: 0);

        Assert.Equal(0xFF445566u, restored[0]);
        Assert.Equal(0x00000000u, restored[1]); // index 200 -> zero padding
        Assert.Equal(0xFF778899u, restored[2]);
    }

    [Fact]
    public void ExpandPalette_AppliesPerChannelPrefixSum()
    {
        // Deltas 0x01010101 accumulate: entry i = i * 0x01010101.
        var raw = new uint[3];
        raw[0] = 0x01010101u;
        raw[1] = 0x01010101u;
        raw[2] = 0x01010101u;
        var palette = ColorIndexingTransform.ExpandPalette(raw, 3, bits: 2);

        Assert.Equal(0x01010101u, palette[0]);
        Assert.Equal(0x02020202u, palette[1]);
        Assert.Equal(0x03030303u, palette[2]);
        Assert.Equal(0u, palette[3]); // padded
    }
}
