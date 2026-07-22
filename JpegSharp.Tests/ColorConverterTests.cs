using JpegSharp.Color;
using Xunit;

namespace JpegSharp.Tests;

public class ColorConverterTests
{
    [Theory]
    [InlineData(255, 255, 255, 255, 128, 128)] // white
    [InlineData(0, 0, 0, 0, 128, 128)]         // black
    [InlineData(128, 128, 128, 128, 128, 128)] // gray -> neutral chroma
    public void RgbToYCbCr_MatchesKnownValues(byte r, byte g, byte b, byte ey, byte ecb, byte ecr)
    {
        ColorConverter.RgbToYCbCr(r, g, b, out var y, out var cb, out var cr);
        Assert.Equal(ey, y);
        Assert.Equal(ecb, cb);
        Assert.Equal(ecr, cr);
    }

    [Fact]
    public void RgbToYCbCr_PureRed_ProducesExpectedLumaAndChroma()
    {
        ColorConverter.RgbToYCbCr(255, 0, 0, out var y, out var cb, out var cr);
        Assert.InRange(y, 75, 77);   // ~76
        Assert.InRange(cb, 84, 86);  // ~85
        Assert.Equal(255, cr);       // clamps at 255
    }

    [Fact]
    public void GrayLevels_HaveNeutralChroma()
    {
        for (var v = 0; v <= 255; v++)
        {
            ColorConverter.RgbToYCbCr((byte)v, (byte)v, (byte)v, out var y, out var cb, out var cr);
            Assert.Equal((byte)v, y);
            Assert.Equal(128, cb);
            Assert.Equal(128, cr);
        }
    }

    [Fact]
    public void RgbYCbCr_RoundTrips_WithinTolerance()
    {
        var rng = new Random(11);
        for (var i = 0; i < 2000; i++)
        {
            byte r = (byte)rng.Next(256), g = (byte)rng.Next(256), b = (byte)rng.Next(256);
            ColorConverter.RgbToYCbCr(r, g, b, out var y, out var cb, out var cr);
            ColorConverter.YCbCrToRgb(y, cb, cr, out var r2, out var g2, out var b2);
            Assert.True(Math.Abs(r - r2) <= 2, $"R {r}->{r2}");
            Assert.True(Math.Abs(g - g2) <= 2, $"G {g}->{g2}");
            Assert.True(Math.Abs(b - b2) <= 2, $"B {b}->{b2}");
        }
    }

    [Fact]
    public void YCbCrToRgb_ClampsOutOfGamut()
    {
        ColorConverter.YCbCrToRgb(255, 0, 255, out var r, out var g, out var b);
        Assert.InRange(r, 0, 255);
        Assert.InRange(g, 0, 255);
        Assert.InRange(b, 0, 255);
    }

    [Fact]
    public void PlanarYCbCrToRgb_MatchesPerPixel()
    {
        var rng = new Random(5);
        const int n = 64;
        var y = new byte[n];
        var cb = new byte[n];
        var cr = new byte[n];
        for (var i = 0; i < n; i++)
        {
            y[i] = (byte)rng.Next(256);
            cb[i] = (byte)rng.Next(256);
            cr[i] = (byte)rng.Next(256);
        }

        var rgb = new byte[n * 3];
        ColorConverter.YCbCrToRgb(y, cb, cr, rgb);

        for (var i = 0; i < n; i++)
        {
            ColorConverter.YCbCrToRgb(y[i], cb[i], cr[i], out var r, out var g, out var b);
            Assert.Equal(r, rgb[i * 3]);
            Assert.Equal(g, rgb[i * 3 + 1]);
            Assert.Equal(b, rgb[i * 3 + 2]);
        }
    }

    [Fact]
    public void PlanarRgbToYCbCr_MatchesPerPixel()
    {
        var rng = new Random(6);
        const int n = 50;
        var rgb = new byte[n * 3];
        rng.NextBytes(rgb);

        var y = new byte[n];
        var cb = new byte[n];
        var cr = new byte[n];
        ColorConverter.RgbToYCbCr(rgb, y, cb, cr);

        for (var i = 0; i < n; i++)
        {
            ColorConverter.RgbToYCbCr(rgb[i * 3], rgb[i * 3 + 1], rgb[i * 3 + 2], out var ey, out var ecb, out var ecr);
            Assert.Equal(ey, y[i]);
            Assert.Equal(ecb, cb[i]);
            Assert.Equal(ecr, cr[i]);
        }
    }

    [Fact]
    public void CmykRgb_RoundTrips_WithinTolerance()
    {
        var rng = new Random(21);
        for (var i = 0; i < 2000; i++)
        {
            byte r = (byte)rng.Next(256), g = (byte)rng.Next(256), b = (byte)rng.Next(256);
            ColorConverter.RgbToCmyk(r, g, b, out var c, out var m, out var yy, out var k);
            ColorConverter.CmykToRgb(c, m, yy, k, out var r2, out var g2, out var b2);
            Assert.True(Math.Abs(r - r2) <= 1);
            Assert.True(Math.Abs(g - g2) <= 1);
            Assert.True(Math.Abs(b - b2) <= 1);
        }
    }

    [Fact]
    public void YcckCmyk_RoundTrips_WithinTolerance()
    {
        var rng = new Random(31);
        for (var i = 0; i < 2000; i++)
        {
            byte c = (byte)rng.Next(256), m = (byte)rng.Next(256), yy = (byte)rng.Next(256), k = (byte)rng.Next(256);
            ColorConverter.CmykToYcck(c, m, yy, k, out var yc, out var cb, out var cr, out var k2);
            Assert.Equal(k, k2); // K passes through untouched
            ColorConverter.YcckToCmyk(yc, cb, cr, k2, out var c2, out var m2, out var y2, out var k3);
            Assert.True(Math.Abs(c - c2) <= 2);
            Assert.True(Math.Abs(m - m2) <= 2);
            Assert.True(Math.Abs(yy - y2) <= 2);
            Assert.Equal(k, k3);
        }
    }
}
