using System.Numerics;
using ZGF.Svg.Parsing;
using ZGF.Svg.Scene;

namespace ZGF.Svg.Tests;

public sealed class PathDataParserTests
{
    private static List<PathSegment> Parse(string d)
    {
        var segments = new List<PathSegment>();
        PathDataParser.Parse(d, segments);
        return segments;
    }

    [Fact]
    public void MoveAndLine_Absolute()
    {
        var segs = Parse("M 10 20 L 30 40");
        Assert.Equal(2, segs.Count);
        Assert.Equal(SegKind.Move, segs[0].Kind);
        Assert.Equal(new Vector2(10, 20), segs[0].P3);
        Assert.Equal(SegKind.Line, segs[1].Kind);
        Assert.Equal(new Vector2(30, 40), segs[1].P3);
    }

    [Fact]
    public void MoveAndLine_Relative()
    {
        var segs = Parse("m 10 20 l 5 5 l -2 3");
        Assert.Equal(new Vector2(10, 20), segs[0].P3);
        Assert.Equal(new Vector2(15, 25), segs[1].P3);
        Assert.Equal(new Vector2(13, 28), segs[2].P3);
    }

    [Fact]
    public void ImplicitLineToAfterMove()
    {
        var segs = Parse("M 0 0 10 10 20 20");
        Assert.Equal(3, segs.Count);
        Assert.Equal(SegKind.Move, segs[0].Kind);
        Assert.Equal(SegKind.Line, segs[1].Kind);
        Assert.Equal(new Vector2(10, 10), segs[1].P3);
        Assert.Equal(SegKind.Line, segs[2].Kind);
    }

    [Fact]
    public void RelativeMoveImplicitLinesAreRelative()
    {
        var segs = Parse("m 10 10 10 0 0 10");
        Assert.Equal(new Vector2(20, 10), segs[1].P3);
        Assert.Equal(new Vector2(20, 20), segs[2].P3);
    }

    [Fact]
    public void HorizontalAndVertical()
    {
        var segs = Parse("M 5 5 H 20 V 30 h -3 v -4");
        Assert.Equal(new Vector2(20, 5), segs[1].P3);
        Assert.Equal(new Vector2(20, 30), segs[2].P3);
        Assert.Equal(new Vector2(17, 30), segs[3].P3);
        Assert.Equal(new Vector2(17, 26), segs[4].P3);
        Assert.All(segs.Skip(1), s => Assert.Equal(SegKind.Line, s.Kind));
    }

    [Fact]
    public void CubicAbsolute()
    {
        var segs = Parse("M 0 0 C 1 2 3 4 5 6");
        Assert.Equal(SegKind.Cubic, segs[1].Kind);
        Assert.Equal(new Vector2(1, 2), segs[1].P1);
        Assert.Equal(new Vector2(3, 4), segs[1].P2);
        Assert.Equal(new Vector2(5, 6), segs[1].P3);
    }

    [Fact]
    public void SmoothCubicReflectsPreviousControl()
    {
        var segs = Parse("M 0 0 C 0 10 10 10 10 0 S 20 -10 20 0");
        Assert.Equal(SegKind.Cubic, segs[2].Kind);
        // Reflection of (10,10) around (10,0) is (10,-10).
        Assert.Equal(new Vector2(10, -10), segs[2].P1);
        Assert.Equal(new Vector2(20, -10), segs[2].P2);
        Assert.Equal(new Vector2(20, 0), segs[2].P3);
    }

    [Fact]
    public void SmoothCubicWithoutPreviousCubicUsesCurrentPoint()
    {
        var segs = Parse("M 5 5 S 20 20 30 5");
        Assert.Equal(SegKind.Cubic, segs[1].Kind);
        Assert.Equal(new Vector2(5, 5), segs[1].P1);
    }

    [Fact]
    public void QuadraticElevatesToCubicExactly()
    {
        var segs = Parse("M 0 0 Q 15 30 30 0");
        Assert.Equal(SegKind.Cubic, segs[1].Kind);
        Assert.Equal(new Vector2(10, 20), segs[1].P1);
        Assert.Equal(new Vector2(20, 20), segs[1].P2);
        Assert.Equal(new Vector2(30, 0), segs[1].P3);
    }

    [Fact]
    public void SmoothQuadraticReflects()
    {
        var segs = Parse("M 0 0 Q 10 20 20 0 T 40 0");
        // Reflected quad control: (30, -20). Elevated c1 = to*1/3 + q*2/3 from (20,0).
        Assert.Equal(SegKind.Cubic, segs[2].Kind);
        var expectedC1 = new Vector2(20, 0) + (new Vector2(30, -20) - new Vector2(20, 0)) * (2f / 3f);
        Assert.Equal(expectedC1.X, segs[2].P1.X, 3);
        Assert.Equal(expectedC1.Y, segs[2].P1.Y, 3);
    }

    [Fact]
    public void ClosePathResetsCurrentPointToSubpathStart()
    {
        var segs = Parse("M 10 10 L 20 10 L 20 20 Z L 0 0");
        Assert.Equal(SegKind.Close, segs[3].Kind);
        // The L after Z starts from (10,10); absolute L target is (0,0).
        Assert.Equal(new Vector2(0, 0), segs[4].P3);
    }

    [Fact]
    public void RelativeAfterCloseUsesSubpathStart()
    {
        var segs = Parse("m 10 10 l 10 0 z l 5 5");
        Assert.Equal(new Vector2(15, 15), segs[3].P3);
    }

    [Fact]
    public void ArcBecomesLineWhenRadiusZero()
    {
        var segs = Parse("M 0 0 A 0 0 0 0 0 10 10");
        Assert.Equal(SegKind.Line, segs[1].Kind);
        Assert.Equal(new Vector2(10, 10), segs[1].P3);
    }

    [Fact]
    public void ArcProducesCubicsEndingAtTarget()
    {
        var segs = Parse("M 0 0 A 10 10 0 0 1 20 0");
        Assert.True(segs.Count >= 2);
        Assert.All(segs.Skip(1), s => Assert.Equal(SegKind.Cubic, s.Kind));
        Assert.Equal(new Vector2(20, 0), segs[^1].P3);
    }

    [Fact]
    public void ArcHalfCircleStaysOnCircle()
    {
        // Half circle radius 10 centered at (10,0), sweep=1.
        var segs = Parse("M 0 0 A 10 10 0 0 1 20 0");
        var center = new Vector2(10, 0);
        // Sample the cubics; all sampled points must be ~radius 10 from center.
        var from = new Vector2(0, 0);
        foreach (var seg in segs.Skip(1))
        {
            for (var i = 0; i <= 8; i++)
            {
                var t = i / 8f;
                var u = 1 - t;
                var p = u * u * u * from + 3 * u * u * t * seg.P1 + 3 * u * t * t * seg.P2 + t * t * t * seg.P3;
                Assert.Equal(10f, (p - center).Length(), 0.05f);
            }
            from = seg.P3;
        }
    }

    [Fact]
    public void PackedArcFlagsParse()
    {
        // "1 1 0 011 0" packs largeArc=0, sweep=1, then x=1 y=0... here: flags "01" then "1 0".
        var segs = Parse("M 0 0 a1 1 0 011 0");
        Assert.True(segs.Count >= 2);
        Assert.Equal(new Vector2(1, 0), segs[^1].P3);
    }

    [Fact]
    public void AdjacentDecimalsParse()
    {
        var segs = Parse("M.5.5L1.5.5");
        Assert.Equal(new Vector2(0.5f, 0.5f), segs[0].P3);
        Assert.Equal(new Vector2(1.5f, 0.5f), segs[1].P3);
    }

    [Fact]
    public void ExponentsParse()
    {
        var segs = Parse("M 1e1 2E-1 L 1.5e2 0");
        Assert.Equal(new Vector2(10f, 0.2f), segs[0].P3);
        Assert.Equal(new Vector2(150f, 0f), segs[1].P3);
    }

    [Fact]
    public void MalformedDataKeepsParsedPrefix()
    {
        var segs = Parse("M 0 0 L 10 10 L banana");
        Assert.Equal(2, segs.Count);
        Assert.Equal(new Vector2(10, 10), segs[1].P3);
    }

    [Fact]
    public void CommaSeparatorsParse()
    {
        var segs = Parse("M10,20L30,40");
        Assert.Equal(new Vector2(10, 20), segs[0].P3);
        Assert.Equal(new Vector2(30, 40), segs[1].P3);
    }

    [Fact]
    public void NegativeNumbersWithoutSeparators()
    {
        var segs = Parse("M10-20L-5-6");
        Assert.Equal(new Vector2(10, -20), segs[0].P3);
        Assert.Equal(new Vector2(-5, -6), segs[1].P3);
    }
}
