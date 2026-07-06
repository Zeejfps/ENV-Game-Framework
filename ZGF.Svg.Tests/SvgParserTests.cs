using ZGF.Svg.Scene;

namespace ZGF.Svg.Tests;

public sealed class SvgParserTests
{
    [Fact]
    public void ParsesViewBoxAndIntrinsicSize()
    {
        var doc = SvgDocument.Parse("""<svg xmlns="http://www.w3.org/2000/svg" width="48" height="24" viewBox="0 0 24 12"><rect width="24" height="12"/></svg>""");
        Assert.Equal(new SvgViewBox(0, 0, 24, 12), doc.ViewBox);
        Assert.Equal(48, doc.IntrinsicWidth);
        Assert.Equal(24, doc.IntrinsicHeight);
    }

    [Fact]
    public void ViewBoxFallsBackToWidthHeight()
    {
        var doc = SvgDocument.Parse("""<svg width="10px" height="20px"><rect width="5" height="5"/></svg>""");
        Assert.Equal(new SvgViewBox(0, 0, 10, 20), doc.ViewBox);
    }

    [Fact]
    public void NoSvgRootThrows()
    {
        Assert.Throws<FormatException>(() => SvgDocument.Parse("<div>hello</div>"));
        Assert.False(SvgDocument.TryParse("<div/>", out _, out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void MalformedXmlReportsViaTryParse()
    {
        Assert.False(SvgDocument.TryParse("<svg><path", out var doc, out var error));
        Assert.Null(doc);
        Assert.NotNull(error);
    }

    [Fact]
    public void DefsContentDoesNotPaint()
    {
        var doc = SvgDocument.Parse("""
            <svg viewBox="0 0 10 10">
              <defs><rect width="10" height="10"/></defs>
              <title>hi</title>
              <style>.a { fill: red; }</style>
            </svg>
            """);
        Assert.Empty(doc.Scene.Commands);
    }

    [Fact]
    public void UnknownElementsAreSkippedButSiblingsRender()
    {
        var doc = SvgDocument.Parse("""
            <svg viewBox="0 0 10 10">
              <filter><feGaussianBlur/></filter>
              <rect width="10" height="10"/>
            </svg>
            """);
        Assert.Single(doc.Scene.Commands);
    }

    [Fact]
    public void FillDefaultsToBlack()
    {
        var doc = SvgDocument.Parse("""<svg viewBox="0 0 10 10"><rect width="5" height="5"/></svg>""");
        var cmd = doc.Scene.Commands[0];
        Assert.Equal(SvgPaintKind.Color, cmd.Fill.Kind);
        Assert.Equal(0xFF000000u, cmd.Fill.ColorArgb);
        Assert.Equal(SvgPaintKind.None, cmd.Stroke.Kind);
    }

    [Fact]
    public void StyleAttributeWinsOverPresentationAttribute()
    {
        var doc = SvgDocument.Parse("""<svg viewBox="0 0 10 10"><rect width="5" height="5" fill="red" style="fill: blue"/></svg>""");
        Assert.Equal(0xFF0000FFu, doc.Scene.Commands[0].Fill.ColorArgb);
    }

    [Fact]
    public void GroupStyleInherits()
    {
        var doc = SvgDocument.Parse("""
            <svg viewBox="0 0 10 10">
              <g fill="lime" stroke="red" stroke-width="3">
                <rect width="5" height="5"/>
                <rect width="5" height="5" fill="blue"/>
              </g>
              <rect width="5" height="5"/>
            </svg>
            """);
        Assert.Equal(3, doc.Scene.Commands.Length);
        Assert.Equal(0xFF00FF00u, doc.Scene.Commands[0].Fill.ColorArgb);
        Assert.Equal(0xFFFF0000u, doc.Scene.Commands[0].Stroke.ColorArgb);
        Assert.Equal(3f, doc.Scene.Commands[0].StrokeWidth);
        Assert.Equal(0xFF0000FFu, doc.Scene.Commands[1].Fill.ColorArgb);
        // Outside the group, back to defaults.
        Assert.Equal(0xFF000000u, doc.Scene.Commands[2].Fill.ColorArgb);
        Assert.Equal(SvgPaintKind.None, doc.Scene.Commands[2].Stroke.Kind);
    }

    [Fact]
    public void OpacityFoldsIntoPaintAlpha()
    {
        var doc = SvgDocument.Parse("""
            <svg viewBox="0 0 10 10">
              <g opacity="0.5"><rect width="5" height="5" fill-opacity="0.5"/></g>
            </svg>
            """);
        var alpha = doc.Scene.Commands[0].Fill.ColorArgb >> 24;
        Assert.Equal(64u, alpha);  // 255 * 0.25, rounded
    }

    [Fact]
    public void CurrentColorTracked()
    {
        var doc = SvgDocument.Parse("""<svg viewBox="0 0 10 10"><rect width="5" height="5" fill="currentColor"/></svg>""");
        Assert.True(doc.UsesCurrentColor);
        Assert.Equal(SvgPaintKind.CurrentColor, doc.Scene.Commands[0].Fill.Kind);

        var doc2 = SvgDocument.Parse("""<svg viewBox="0 0 10 10"><rect width="5" height="5" fill="red"/></svg>""");
        Assert.False(doc2.UsesCurrentColor);
    }

    [Fact]
    public void CurrentColorResolvesWithFoldedOpacity()
    {
        var doc = SvgDocument.Parse("""<svg viewBox="0 0 10 10"><rect width="5" height="5" fill="currentColor" fill-opacity="0.5"/></svg>""");
        var resolved = doc.Scene.Commands[0].Fill.Resolve(0xFF112233);
        Assert.Equal(0x80112233u, resolved);
    }

    [Fact]
    public void DisplayNoneSkipsElementAndSubtree()
    {
        var doc = SvgDocument.Parse("""
            <svg viewBox="0 0 10 10">
              <rect width="5" height="5" display="none"/>
              <g display="none"><rect width="5" height="5"/></g>
              <rect width="5" height="5"/>
            </svg>
            """);
        Assert.Single(doc.Scene.Commands);
    }

    [Fact]
    public void FillNoneStrokeNoneShapeIsDropped()
    {
        var doc = SvgDocument.Parse("""<svg viewBox="0 0 10 10"><rect width="5" height="5" fill="none"/></svg>""");
        Assert.Empty(doc.Scene.Commands);
        Assert.Empty(doc.Scene.Segments);
    }

    [Fact]
    public void TransformsComposeThroughGroups()
    {
        var doc = SvgDocument.Parse("""
            <svg viewBox="0 0 10 10">
              <g transform="translate(10, 0)">
                <rect transform="scale(2)" width="5" height="5"/>
              </g>
            </svg>
            """);
        var m = doc.Scene.Commands[0].Transform;
        // Point (1,1) → scale → (2,2) → translate → (12,2)
        var p = System.Numerics.Vector2.Transform(new System.Numerics.Vector2(1, 1), m);
        Assert.Equal(12f, p.X, 3);
        Assert.Equal(2f, p.Y, 3);
    }

    [Fact]
    public void DashArrayOddCountRepeats()
    {
        var doc = SvgDocument.Parse("""<svg viewBox="0 0 10 10"><line x1="0" y1="0" x2="10" y2="0" stroke="black" stroke-dasharray="1 2 3"/></svg>""");
        var cmd = doc.Scene.Commands[0];
        Assert.Equal(6, cmd.DashCount);
        Assert.Equal([1f, 2f, 3f, 1f, 2f, 3f], doc.Scene.DashValues.AsSpan(cmd.DashStart, cmd.DashCount).ToArray());
    }

    [Fact]
    public void NegativeDashValueDisablesDashing()
    {
        var doc = SvgDocument.Parse("""<svg viewBox="0 0 10 10"><line x1="0" y1="0" x2="10" y2="0" stroke="black" stroke-dasharray="4 -1"/></svg>""");
        Assert.Equal(0, doc.Scene.Commands[0].DashCount);
    }

    [Fact]
    public void LineHasNoFill()
    {
        var doc = SvgDocument.Parse("""<svg viewBox="0 0 10 10"><line x1="0" y1="0" x2="10" y2="10" stroke="red"/></svg>""");
        var cmd = doc.Scene.Commands[0];
        Assert.Equal(SvgPaintKind.None, cmd.Fill.Kind);
        Assert.Equal(SvgPaintKind.Color, cmd.Stroke.Kind);
    }

    [Fact]
    public void BasicShapesLowerToSegments()
    {
        var doc = SvgDocument.Parse("""
            <svg viewBox="0 0 100 100">
              <circle cx="50" cy="50" r="20"/>
              <ellipse cx="50" cy="50" rx="20" ry="10"/>
              <polygon points="0,0 10,0 5,10"/>
              <polyline points="0,0 10,0 5,10" fill="none" stroke="red"/>
              <rect x="1" y="2" width="8" height="4" rx="1"/>
            </svg>
            """);
        Assert.Equal(5, doc.Scene.Commands.Length);
        // Polygon closes, polyline doesn't.
        var polygonSegs = doc.Scene.Segments.AsSpan(doc.Scene.Commands[2].SegStart, doc.Scene.Commands[2].SegCount);
        Assert.Equal(SegKind.Close, polygonSegs[^1].Kind);
        var polylineSegs = doc.Scene.Segments.AsSpan(doc.Scene.Commands[3].SegStart, doc.Scene.Commands[3].SegCount);
        Assert.NotEqual(SegKind.Close, polylineSegs[^1].Kind);
    }

    [Fact]
    public void EvenOddFillRuleParses()
    {
        var doc = SvgDocument.Parse("""<svg viewBox="0 0 10 10"><path d="M0 0h10v10h-10z" fill-rule="evenodd"/></svg>""");
        Assert.Equal(SvgFillRule.EvenOdd, doc.Scene.Commands[0].FillRule);
    }

    [Fact]
    public void StrokePropertiesParse()
    {
        var doc = SvgDocument.Parse("""
            <svg viewBox="0 0 10 10">
              <path d="M0 0 L10 10" stroke="black" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="bevel" stroke-miterlimit="8" fill="none"/>
            </svg>
            """);
        var cmd = doc.Scene.Commands[0];
        Assert.Equal(2.5f, cmd.StrokeWidth);
        Assert.Equal(SvgLineCap.Round, cmd.Cap);
        Assert.Equal(SvgLineJoin.Bevel, cmd.Join);
        Assert.Equal(8f, cmd.MiterLimit);
    }

    [Fact]
    public void CommentsAndDoctypeAreTolerated()
    {
        var doc = SvgDocument.Parse("""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE svg PUBLIC "-//W3C//DTD SVG 1.1//EN" "http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd">
            <!-- a comment -->
            <svg viewBox="0 0 10 10"><rect width="5" height="5"/></svg>
            """);
        Assert.Single(doc.Scene.Commands);
    }

    [Fact]
    public void Utf8OverloadParses()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("""<svg viewBox="0 0 10 10"><rect width="5" height="5"/></svg>""");
        var doc = SvgDocument.Parse(bytes);
        Assert.Single(doc.Scene.Commands);
    }
}
