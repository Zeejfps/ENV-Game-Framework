using JpegSharp.Quantization;
using Xunit;

namespace JpegSharp.Tests;

public class QuantizationTests
{
    [Fact]
    public void Dequantize_MultipliesCoefficientsByTable()
    {
        Span<short> quantized = stackalloc short[64];
        Span<ushort> table = stackalloc ushort[64];
        for (var i = 0; i < 64; i++)
        {
            quantized[i] = (short)(i - 32);
            table[i] = (ushort)(i + 1);
        }

        Span<double> output = stackalloc double[64];
        Quantizer.Dequantize(quantized, table, output);

        for (var i = 0; i < 64; i++)
            Assert.Equal(quantized[i] * (double)table[i], output[i], 9);
    }

    [Fact]
    public void Quantize_DividesAndRoundsToNearest()
    {
        Span<double> coeffs = stackalloc double[64];
        Span<ushort> table = stackalloc ushort[64];
        table.Fill(10);
        coeffs[0] = 104; // 10.4 -> 10
        coeffs[1] = 105; // 10.5 -> 11 (away from zero)
        coeffs[2] = -105; // -10.5 -> -11
        coeffs[3] = -104; // -10.4 -> -10

        Span<short> output = stackalloc short[64];
        Quantizer.Quantize(coeffs, table, output);

        Assert.Equal(10, output[0]);
        Assert.Equal(11, output[1]);
        Assert.Equal(-11, output[2]);
        Assert.Equal(-10, output[3]);
    }

    [Fact]
    public void QuantizeThenDequantize_ApproximatesOriginal()
    {
        Span<double> coeffs = stackalloc double[64];
        var rng = new Random(99);
        for (var i = 0; i < 64; i++)
            coeffs[i] = rng.Next(-500, 500);

        Span<ushort> table = stackalloc ushort[64];
        table.Fill(8);

        Span<short> q = stackalloc short[64];
        Span<double> back = stackalloc double[64];
        Quantizer.Quantize(coeffs, table, q);
        Quantizer.Dequantize(q, table, back);

        // Reconstruction error is bounded by half a quantization step.
        for (var i = 0; i < 64; i++)
            Assert.True(Math.Abs(coeffs[i] - back[i]) <= 8 / 2.0 + 1e-9);
    }

    [Fact]
    public void StandardLuminance_Quality50_EqualsBaseTable()
    {
        var table = QuantizationTable.Luminance(50);
        Assert.Equal(16, table[0]);
        Assert.Equal(11, table[1]);
        Assert.Equal(99, table[63]);
    }

    [Fact]
    public void StandardChrominance_Quality50_EqualsBaseTable()
    {
        var table = QuantizationTable.Chrominance(50);
        Assert.Equal(17, table[0]);
        Assert.Equal(18, table[1]);
        Assert.Equal(99, table[63]);
    }

    [Fact]
    public void Quality100_ProducesAllOnes()
    {
        var table = QuantizationTable.Luminance(100);
        for (var i = 0; i < 64; i++)
            Assert.Equal(1, table[i]);
    }

    [Fact]
    public void LowerQuality_ProducesLargerOrEqualQuantizationSteps()
    {
        var high = QuantizationTable.Luminance(90);
        var low = QuantizationTable.Luminance(10);
        for (var i = 0; i < 64; i++)
            Assert.True(low[i] >= high[i], $"low[{i}]={low[i]} high[{i}]={high[i]}");
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(200, 100)]
    public void Quality_IsClamped(int requested, int equivalent)
    {
        var a = QuantizationTable.Luminance(requested);
        var b = QuantizationTable.Luminance(equivalent);
        for (var i = 0; i < 64; i++)
            Assert.Equal(b[i], a[i]);
    }

    [Fact]
    public void QuantizationValues_AreClampedToByteRange()
    {
        var table = QuantizationTable.Luminance(1);
        for (var i = 0; i < 64; i++)
            Assert.InRange(table[i], 1, 255);
    }

    [Fact]
    public void ScaleValue_8bit_ClampsTo255()
    {
        var scale = StandardQuantizationTables.QualityToScale(1);
        ReadOnlySpan<ushort> baseTable = StandardQuantizationTables.Luminance;
        for (var i = 0; i < 64; i++)
        {
            var v = StandardQuantizationTables.ScaleValue(baseTable[i], scale);
            Assert.InRange(v, (ushort)1, (ushort)255);
        }
    }

    [Fact]
    public void ScaleValue_12bit_AllowsUpTo65535()
    {
        var scale = StandardQuantizationTables.QualityToScale(1);
        ReadOnlySpan<ushort> baseTable = StandardQuantizationTables.Luminance;
        var sawAbove255 = false;
        for (var i = 0; i < 64; i++)
        {
            var v = StandardQuantizationTables.ScaleValue(baseTable[i], scale, samplePrecision: 12);
            Assert.InRange(v, (ushort)1, (ushort)65535);
            if (v > 255)
                sawAbove255 = true;
        }

        Assert.True(sawAbove255, "12-bit low-quality scaling must produce at least one step > 255.");
    }

    [Fact]
    public void Quantize_LargeCoefficient_ClampsToShortRange()
    {
        Span<double> coeffs = stackalloc double[64];
        Span<ushort> table = stackalloc ushort[64];
        table.Fill(1);
        coeffs[0] = 100000.0;  // 100000 / 1 exceeds short.MaxValue (32767)
        coeffs[1] = -100000.0; // symmetric negative case

        Span<short> output = stackalloc short[64];
        Quantizer.Quantize(coeffs, table, output);

        Assert.Equal(short.MaxValue, output[0]);
        Assert.Equal(short.MinValue, output[1]);
    }

    [Fact]
    public void CustomTable_PreservesValuesInNaturalOrder()
    {
        var values = new ushort[64];
        for (var i = 0; i < 64; i++)
            values[i] = (ushort)(i + 1);

        var table = new QuantizationTable(values);
        for (var i = 0; i < 64; i++)
            Assert.Equal((ushort)(i + 1), table[i]);
    }

    [Fact]
    public void CustomTable_WrongLength_Throws()
    {
        Assert.Throws<ArgumentException>(() => new QuantizationTable(new ushort[63]));
    }

    [Fact]
    public void CustomTable_ZeroValue_Throws()
    {
        var values = new ushort[64];
        Array.Fill(values, (ushort)1);
        values[10] = 0;
        Assert.Throws<ArgumentException>(() => new QuantizationTable(values));
    }

    [Fact]
    public void ZigZagRoundTrip_PreservesTable()
    {
        var table = QuantizationTable.Luminance(75);
        Span<ushort> zig = stackalloc ushort[64];
        table.CopyToZigZag(zig);
        var restored = QuantizationTable.FromZigZag(zig);
        for (var i = 0; i < 64; i++)
            Assert.Equal(table[i], restored[i]);
    }
}
