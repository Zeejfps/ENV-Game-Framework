using System.Numerics;
using ZGF.Svg.Parsing;

namespace ZGF.Svg.Tests;

public sealed class ColorParserTests
{
    [Theory]
    [InlineData("#fff", 0xFFFFFFFFu)]
    [InlineData("#000", 0xFF000000u)]
    [InlineData("#f00", 0xFFFF0000u)]
    [InlineData("#ff0000", 0xFFFF0000u)]
    [InlineData("#00ff0080", 0x8000FF00u)]
    [InlineData("#1234", 0x44112233u)]
    [InlineData("red", 0xFFFF0000u)]
    [InlineData("Black", 0xFF000000u)]
    [InlineData("CORNFLOWERBLUE", 0xFF6495EDu)]
    [InlineData("transparent", 0x00000000u)]
    [InlineData("rgb(255, 0, 0)", 0xFFFF0000u)]
    [InlineData("rgb(100%, 0%, 50%)", 0xFFFF0080u)]
    [InlineData("rgba(0, 255, 0, 0.5)", 0x8000FF00u)]
    public void ParsesColors(string input, uint expected)
    {
        Assert.True(ColorParser.TryParseColor(input, out var argb));
        Assert.Equal(expected, argb);
    }

    [Fact]
    public void NoneIsPaintKindNone()
    {
        Assert.True(ColorParser.TryParsePaint("none", out var kind, out _));
        Assert.Equal(SvgPaintKind.None, kind);
    }

    [Fact]
    public void CurrentColorIsPaintKind()
    {
        Assert.True(ColorParser.TryParsePaint("currentColor", out var kind, out _));
        Assert.Equal(SvgPaintKind.CurrentColor, kind);
    }

    [Theory]
    [InlineData("url(#gradient)")]
    [InlineData("bogus-color-name")]
    [InlineData("")]
    public void UnrecognizedValuesReturnFalse(string input)
    {
        Assert.False(ColorParser.TryParsePaint(input, out _, out _));
    }
}

public sealed class TransformParserTests
{
    private static void AssertTransformsTo(Matrix3x2 m, Vector2 input, Vector2 expected, float tolerance = 1e-3f)
    {
        var actual = Vector2.Transform(input, m);
        Assert.Equal(expected.X, actual.X, tolerance);
        Assert.Equal(expected.Y, actual.Y, tolerance);
    }

    [Fact]
    public void Translate()
    {
        var m = TransformParser.Parse("translate(10, 20)");
        AssertTransformsTo(m, Vector2.Zero, new Vector2(10, 20));
    }

    [Fact]
    public void TranslateSingleArgHasZeroY()
    {
        var m = TransformParser.Parse("translate(10)");
        AssertTransformsTo(m, Vector2.Zero, new Vector2(10, 0));
    }

    [Fact]
    public void ScaleSingleArgIsUniform()
    {
        var m = TransformParser.Parse("scale(2)");
        AssertTransformsTo(m, new Vector2(3, 4), new Vector2(6, 8));
    }

    [Fact]
    public void Rotate90()
    {
        var m = TransformParser.Parse("rotate(90)");
        AssertTransformsTo(m, new Vector2(1, 0), new Vector2(0, 1));
    }

    [Fact]
    public void RotateAboutPoint()
    {
        var m = TransformParser.Parse("rotate(90, 10, 10)");
        AssertTransformsTo(m, new Vector2(20, 10), new Vector2(10, 20));
    }

    [Fact]
    public void Matrix()
    {
        var m = TransformParser.Parse("matrix(1, 2, 3, 4, 5, 6)");
        // p' = (x*a + y*c + e, x*b + y*d + f)
        AssertTransformsTo(m, new Vector2(1, 1), new Vector2(1 + 3 + 5, 2 + 4 + 6));
    }

    [Fact]
    public void SkewX()
    {
        var m = TransformParser.Parse("skewX(45)");
        AssertTransformsTo(m, new Vector2(0, 10), new Vector2(10, 10));
    }

    [Fact]
    public void ListAppliesLeftToRight()
    {
        // Per spec, "translate(10,0) scale(2)" == <g translate><g scale>: scale first, then translate.
        var m = TransformParser.Parse("translate(10, 0) scale(2)");
        AssertTransformsTo(m, new Vector2(1, 0), new Vector2(12, 0));
    }

    [Fact]
    public void MalformedItemIsSkipped()
    {
        var m = TransformParser.Parse("bogus(1) translate(5, 5)");
        AssertTransformsTo(m, Vector2.Zero, new Vector2(5, 5));
    }
}
