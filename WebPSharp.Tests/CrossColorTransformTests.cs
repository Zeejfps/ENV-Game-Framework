using WebPSharp.Api;
using WebPSharp.Vp8L;
using WebPSharp.Vp8L.Transforms;

namespace WebPSharp.Tests;

public class CrossColorTransformTests
{
    [Fact]
    public void Inverse_AppliesGreenToRedDelta()
    {
        // multipliers: green_to_red=8, others 0. green=64 -> delta = (8*64)>>5 = 16.
        var argb = new[] { (0xFFu << 24) | (100u << 16) | (64u << 8) | 50u };
        var colorImage = new[] { (0xFFu << 24) | (0u << 16) | (0u << 8) | 8u };
        CrossColorTransform.Inverse(argb, 1, 1, colorImage, 2);

        Assert.Equal(116u, (argb[0] >> 16) & 0xFF); // red += 16
        Assert.Equal(64u, (argb[0] >> 8) & 0xFF);   // green unchanged
        Assert.Equal(50u, argb[0] & 0xFF);          // blue unchanged (g2b=r2b=0)
    }

    [Fact]
    public void Inverse_SignedMultipliersAndSignedChannels_Vector()
    {
        // Exercises the sign semantics: green byte > 127 is negative, new_red feeds red_to_blue as sbyte,
        // and Delta = (sbyte * sbyte) >> 5 (arithmetic shift).
        // pixel: A=255 R=100 G=144(-112) B=32. multipliers: g2r=-8, g2b=16, r2b=-4.
        //   Delta(g2r,green)=(-8*-112)>>5 = 896>>5 = 28  -> newRed = (100+28)&0xFF = 128 (as sbyte -128)
        //   Delta(g2b,green)=(16*-112)>>5 = -1792>>5 = -56
        //   Delta(r2b,newRed)=(-4*-128)>>5 = 512>>5 = 16
        //   newBlue = (32 - 56 + 16) & 0xFF = -8 & 0xFF = 248
        // green unchanged (144), alpha unchanged.
        var argb = new[] { (0xFFu << 24) | (100u << 16) | (144u << 8) | 32u };
        var colorImage = new[] { (0xFFu << 24) | (0xFCu << 16) | (0x10u << 8) | 0xF8u };
        CrossColorTransform.Inverse(argb, 1, 1, colorImage, 2);

        Assert.Equal(0xFF8090F8u, argb[0]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(42)]
    public void ForwardInverse_IsIdentity(int seed)
    {
        const int width = 23, height = 17, bits = 2;
        var rng = new Random(seed);
        var argb = new uint[width * height];
        for (var i = 0; i < argb.Length; i++)
            argb[i] = (uint)rng.NextInt64(0, 1L << 32);

        var subW = Vp8LSubSample.Size(width, bits);
        var subH = Vp8LSubSample.Size(height, bits);
        var colorImage = new uint[subW * subH];
        for (var i = 0; i < colorImage.Length; i++)
        {
            var g2r = (uint)rng.Next(256);
            var g2b = (uint)rng.Next(256);
            var r2b = (uint)rng.Next(256);
            colorImage[i] = (0xFFu << 24) | (r2b << 16) | (g2b << 8) | g2r;
        }

        var work = (uint[])argb.Clone();
        CrossColorTransform.Forward(work, width, height, colorImage, bits);
        CrossColorTransform.Inverse(work, width, height, colorImage, bits);

        Assert.Equal(argb, work);
    }

    [Theory]
    [InlineData(16, 32, 200)]
    [InlineData(240, 0, 8)]
    public void EncoderRoundTrip_WithNonTrivialMultipliers(int g2r, int g2b, int r2b)
    {
        const int w = 30, h = 20;
        var rng = new Random(g2r + g2b + r2b);
        var pixels = new byte[w * h * 4];
        rng.NextBytes(pixels);
        var image = WebPImage.CreateRgba(w, h, pixels);

        var payload = Vp8LEncoder.Encode(image, new Vp8LEncodeSettings
        {
            CrossColor = true,
            CrossColorGreenToRed = (byte)g2r,
            CrossColorGreenToBlue = (byte)g2b,
            CrossColorRedToBlue = (byte)r2b,
        });
        var decoded = Vp8LDecoder.Decode(payload);

        Assert.Equal(pixels, decoded.PixelData);
    }
}
