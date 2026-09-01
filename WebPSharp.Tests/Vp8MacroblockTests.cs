using WebPSharp.Container;
using WebPSharp.Vp8;

namespace WebPSharp.Tests;

public class Vp8MacroblockTests
{
    private static Vp8Decoder DecodeGolden(string asset = "grad_q80.webp")
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", asset);
        var bytes = File.ReadAllBytes(path);
        var reader = RiffReader.Create(bytes);
        byte[]? payload = null;
        while (reader.MoveNext())
            if (reader.Current.Id == WebPChunkIds.Vp8)
                payload = reader.Current.Payload.ToArray();
        var decoder = new Vp8Decoder(payload!);
        decoder.ParseHeaders();
        decoder.DecodeMacroblocks();
        return decoder;
    }

    [Fact]
    public void DecodesAllMacroblocks_WithoutDesync()
    {
        var d = DecodeGolden();
        Assert.Equal(4 * 3, d.MbWidth * d.MbHeight);

        // The mode partition must not have run dry: a boolean-decoder desync during mode/coeff
        // parsing would exhaust it early.
        Assert.False(d.FirstPartition.IsEndOfStream);
    }

    [Fact]
    public void ModesAreInValidRanges()
    {
        var d = DecodeGolden();
        var count = d.MbWidth * d.MbHeight;
        for (var mb = 0; mb < count; mb++)
        {
            Assert.InRange(d.MbUvMode[mb], 0, 3);
            Assert.InRange(d.MbSegment[mb], 0, 3);
            if (d.MbIsI4x4[mb])
            {
                for (var i = 0; i < 16; i++)
                    Assert.InRange(d.MbModes[mb * 16 + i], 0, 9);
            }
            else
            {
                Assert.InRange(d.MbModes[mb * 16], 0, 3); // 16x16 Y mode
            }
        }
    }

    [Fact]
    public void ProducesNonZeroCoefficients()
    {
        var d = DecodeGolden();
        // A quality-80 gradient will have plenty of non-zero DCT coefficients.
        var anyNonZero = false;
        foreach (var c in d.MbCoeffs)
            if (c != 0) { anyNonZero = true; break; }
        Assert.True(anyNonZero, "Expected some non-zero coefficients in a q80 image.");

        var mbsWithCoeffs = 0;
        for (var mb = 0; mb < d.MbWidth * d.MbHeight; mb++)
            if (d.MbNonZeroY[mb] != 0 || d.MbNonZeroUv[mb] != 0)
                mbsWithCoeffs++;
        Assert.True(mbsWithCoeffs > 0);
    }

    [Fact]
    public void CoefficientsAreDequantizedToPlausibleMagnitudes()
    {
        var d = DecodeGolden();
        // Dequantized residual coefficients should never exceed a sane bound.
        foreach (var c in d.MbCoeffs)
            Assert.InRange((int)c, -8192, 8192);
    }
}
