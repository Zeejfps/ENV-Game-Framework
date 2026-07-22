using WebPSharp.Api;
using WebPSharp.Vp8L;

namespace WebPSharp.Tests;

public class Vp8LRoundTripTests
{
    private static void AssertRoundTrips(WebPImage image)
    {
        var payload = Vp8LEncoder.Encode(image);
        var decoded = Vp8LDecoder.Decode(payload);

        Assert.Equal(image.Width, decoded.Width);
        Assert.Equal(image.Height, decoded.Height);
        Assert.Equal(WebPColorFormat.Rgba, decoded.Format);

        // Compare against the source promoted to RGBA (RGB sources gain opaque alpha).
        var expected = ToRgba(image);
        Assert.Equal(expected, decoded.PixelData);
    }

    private static byte[] ToRgba(WebPImage image)
    {
        if (image.Format == WebPColorFormat.Rgba)
            return image.PixelData;

        var rgba = new byte[image.Width * image.Height * 4];
        var src = image.PixelData;
        for (int i = 0, j = 0; i < src.Length; i += 3, j += 4)
        {
            rgba[j] = src[i];
            rgba[j + 1] = src[i + 1];
            rgba[j + 2] = src[i + 2];
            rgba[j + 3] = 255;
        }
        return rgba;
    }

    [Fact]
    public void SinglePixel_Rgba()
    {
        AssertRoundTrips(WebPImage.CreateRgba(1, 1, new byte[] { 12, 34, 56, 78 }));
    }

    [Fact]
    public void SolidColor_Rgba()
    {
        var pixels = new byte[8 * 8 * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = 200;
            pixels[i + 1] = 100;
            pixels[i + 2] = 50;
            pixels[i + 3] = 255;
        }
        AssertRoundTrips(WebPImage.CreateRgba(8, 8, pixels));
    }

    [Fact]
    public void Rgb_Source_GainsOpaqueAlpha()
    {
        var rng = new Random(1);
        var pixels = new byte[5 * 7 * 3];
        rng.NextBytes(pixels);
        AssertRoundTrips(WebPImage.CreateRgb(5, 7, pixels));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(1, 17)]
    [InlineData(17, 1)]
    [InlineData(13, 13)]
    [InlineData(64, 48)]
    [InlineData(100, 1)]
    public void RandomNoise_Rgba_RoundTrips(int width, int height)
    {
        var rng = new Random(width * 7919 + height);
        var pixels = new byte[width * height * 4];
        rng.NextBytes(pixels);
        AssertRoundTrips(WebPImage.CreateRgba(width, height, pixels));
    }

    [Fact]
    public void Gradient_Rgba_RoundTrips()
    {
        const int w = 32, h = 32;
        var pixels = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var i = (y * w + x) * 4;
            pixels[i] = (byte)(x * 8);
            pixels[i + 1] = (byte)(y * 8);
            pixels[i + 2] = (byte)(x + y);
            pixels[i + 3] = (byte)(255 - x);
        }
        AssertRoundTrips(WebPImage.CreateRgba(w, h, pixels));
    }

    [Fact]
    public void FullyTransparent_RoundTrips()
    {
        var pixels = new byte[16 * 16 * 4]; // all zero -> transparent black
        AssertRoundTrips(WebPImage.CreateRgba(16, 16, pixels));
    }

    [Fact]
    public void DeterministicOutput_SameInputSameBytes()
    {
        var rng = new Random(42);
        var pixels = new byte[20 * 20 * 4];
        rng.NextBytes(pixels);
        var image = WebPImage.CreateRgba(20, 20, pixels);
        Assert.Equal(Vp8LEncoder.Encode(image), Vp8LEncoder.Encode(image));
    }

    [Fact]
    public void Decode_ExercisesLz77CopyAndColorCache()
    {
        // Hand-craft a tiny VP8L stream: one literal pixel then a back-reference copying it,
        // plus a color-cache hit, to cover the decoder paths the literal encoder never emits.
        // Uses a distance plane code > 120 so no near-distance table is required.
        const int width = 4, height = 1; // 4 pixels in one row
        var w = new Vp8LBitWriter();
        w.PutBits(0x2F, 8);
        w.PutBits(width - 1, 14);
        w.PutBits(height - 1, 14);
        w.PutBits(0, 1); // alpha not used
        w.PutBits(0, 3); // version

        w.PutBit(0); // no transforms
        w.PutBit(1); // color cache present
        w.PutBits(1, 4); // cache bits = 1 -> 2 entries
        w.PutBit(0); // no meta huffman

        // Green alphabet = 256 + 24 + 2 (cache) = 282.
        const int cacheSize = 2;
        const int greenAlpha = 256 + 24 + cacheSize;

        // Green code: symbols we use are literal green 0x40, length symbol (256+0), and cache
        // index symbol (256+24+0). Give each a short code via an explicit length table.
        var greenLengths = new int[greenAlpha];
        greenLengths[0x40] = 2;            // literal green value
        greenLengths[256 + 0] = 2;         // LZ77 length prefix code 0 (length in [1..1] region)
        greenLengths[256 + 24 + 0] = 2;    // color cache index 0
        greenLengths[256 + 24 + 1] = 2;    // pad to a complete code (unused but present)
        Vp8LHuffman.WritePrefixCode(w, greenLengths);

        var redLengths = new int[256]; redLengths[0x10] = 1; redLengths[0x11] = 1;
        Vp8LHuffman.WritePrefixCode(w, redLengths);
        var blueLengths = new int[256]; blueLengths[0x20] = 1; blueLengths[0x21] = 1;
        Vp8LHuffman.WritePrefixCode(w, blueLengths);
        var alphaLengths = new int[256]; alphaLengths[0xFF] = 1; alphaLengths[0xFE] = 1;
        Vp8LHuffman.WritePrefixCode(w, alphaLengths);
        // Plane code 121 encodes to distance prefix symbol 13; include it in the code.
        var distLengths = new int[40]; distLengths[13] = 1; distLengths[0] = 1;
        Vp8LHuffman.WritePrefixCode(w, distLengths);

        var green = new PrefixCodeWriter(greenLengths);
        var red = new PrefixCodeWriter(redLengths);
        var blue = new PrefixCodeWriter(blueLengths);
        var alpha = new PrefixCodeWriter(alphaLengths);
        var dist = new PrefixCodeWriter(distLengths);

        // Pixel 0: literal ARGB = (A=0xFF, R=0x10, G=0x40, B=0x20).
        green.WriteSymbol(w, 0x40);
        red.WriteSymbol(w, 0x10);
        blue.WriteSymbol(w, 0x20);
        alpha.WriteSymbol(w, 0xFF);

        // Pixel 1: back-reference length 1, distance plane code 121 -> distance 1 (copies pixel 0).
        green.WriteSymbol(w, 256 + 0);          // length prefix code 0 -> length 1
        LzPrefixEmit(w, dist, planeCode: 121);  // distance

        // Pixel 2: another literal so a distinct color enters the cache.
        green.WriteSymbol(w, 0x40);
        red.WriteSymbol(w, 0x11);
        blue.WriteSymbol(w, 0x21);
        alpha.WriteSymbol(w, 0xFE);

        // Pixel 3: color-cache hit for pixel 2's color.
        var pixel2 = (0xFEu << 24) | (0x11u << 16) | (0x40u << 8) | 0x21u;
        var cacheIndex = new ColorCache(1).GetIndex(pixel2);
        green.WriteSymbol(w, 256 + 24 + cacheIndex);

        var image = Vp8LDecoder.Decode(w.ToArray());
        Assert.Equal(4, image.Width);

        var px = image.PixelData;
        // pixel 0
        Assert.Equal(new byte[] { 0x10, 0x40, 0x20, 0xFF }, px[0..4]);
        // pixel 1 == pixel 0 (copied)
        Assert.Equal(new byte[] { 0x10, 0x40, 0x20, 0xFF }, px[4..8]);
        // pixel 2
        Assert.Equal(new byte[] { 0x11, 0x40, 0x21, 0xFE }, px[8..12]);
        // pixel 3 == pixel 2 (cache hit)
        Assert.Equal(new byte[] { 0x11, 0x40, 0x21, 0xFE }, px[12..16]);
    }

    private static void LzPrefixEmit(Vp8LBitWriter w, PrefixCodeWriter distWriter, int planeCode)
    {
        LzPrefix.Encode(planeCode, out var prefixCode, out var extraBits, out var extraValue);
        distWriter.WriteSymbol(w, prefixCode);
        w.PutBits((uint)extraValue, extraBits);
    }
}
