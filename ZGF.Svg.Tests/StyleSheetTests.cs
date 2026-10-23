namespace ZGF.Svg.Tests;

public sealed class StyleSheetTests
{
    private static uint FillOf(string svg, int command = 0) => SvgDocument.Parse(svg).Scene.Commands[command].Fill.ColorArgb;

    [Fact]
    public void ClassRulesPaintTheirElements()
    {
        var fill = FillOf("""
            <svg viewBox="0 0 10 10">
              <style>.a { fill: #00ff00; }</style>
              <rect class="a" width="10" height="10"/>
            </svg>
            """);
        Assert.Equal(0xFF00FF00u, fill);
    }

    [Fact]
    public void AStyleElementAfterTheShapesStillApplies()
    {
        var fill = FillOf("""
            <svg viewBox="0 0 10 10">
              <rect class="a" width="10" height="10"/>
              <style>.a { fill: blue }</style>
            </svg>
            """);
        Assert.Equal(0xFF0000FFu, fill);
    }

    [Fact]
    public void InlineStyleBeatsRulesAndRulesBeatAttributes()
    {
        var doc = SvgDocument.Parse("""
            <svg viewBox="0 0 10 10">
              <style>.a { fill: red; }</style>
              <rect class="a" fill="blue" width="10" height="10"/>
              <rect class="a" style="fill: lime" width="10" height="10"/>
            </svg>
            """);
        Assert.Equal(0xFFFF0000u, doc.Scene.Commands[0].Fill.ColorArgb);
        Assert.Equal(0xFF00FF00u, doc.Scene.Commands[1].Fill.ColorArgb);
    }

    [Fact]
    public void MoreSpecificSelectorsWinWhateverTheirOrder()
    {
        var fill = FillOf("""
            <svg viewBox="0 0 10 10">
              <style>#x { fill: red } rect.a { fill: blue } .a { fill: lime } rect { fill: black }</style>
              <rect id="x" class="a" width="10" height="10"/>
            </svg>
            """);
        Assert.Equal(0xFFFF0000u, fill);
    }

    [Fact]
    public void LaterRulesOfEqualSpecificityWin()
    {
        var fill = FillOf("""
            <svg viewBox="0 0 10 10">
              <style>.a { fill: red } .b { fill: blue }</style>
              <rect class="a b" width="10" height="10"/>
            </svg>
            """);
        Assert.Equal(0xFF0000FFu, fill);
    }

    [Fact]
    public void SelectorListsAndCompoundClassesMatch()
    {
        var doc = SvgDocument.Parse("""
            <svg viewBox="0 0 10 10">
              <style>.a, .b { fill: blue } .a.c { fill: red }</style>
              <rect class="b" width="10" height="10"/>
              <rect class="c a" width="10" height="10"/>
            </svg>
            """);
        Assert.Equal(0xFF0000FFu, doc.Scene.Commands[0].Fill.ColorArgb);
        Assert.Equal(0xFFFF0000u, doc.Scene.Commands[1].Fill.ColorArgb);
    }

    [Fact]
    public void ClassOpacityHidesATransparentCanvas()
    {
        var doc = SvgDocument.Parse("""
            <svg viewBox="0 0 16 16">
              <style>.canvas{opacity:0;fill:#f6f6f6}.bg{fill:#424242}</style>
              <path class="canvas" d="M16 16H0V0h16v16z"/>
              <path class="bg" d="M4 4h8v8H4z"/>
            </svg>
            """);
        var rgba = doc.Rasterize(16, 16);
        Assert.Equal(0, rgba[3]);
        var center = (8 * 16 + 8) * 4;
        Assert.Equal([0x42, 0x42, 0x42, 0xFF], rgba[center..(center + 4)]);
    }

    [Fact]
    public void GroupRulesAreInherited()
    {
        var fill = FillOf("""
            <svg viewBox="0 0 10 10">
              <style>g.layer { fill: blue }</style>
              <g class="layer"><rect width="10" height="10"/></g>
            </svg>
            """);
        Assert.Equal(0xFF0000FFu, fill);
    }

    [Fact]
    public void UnsupportedSelectorsAtRulesAndCommentsAreSkipped()
    {
        var fill = FillOf("""
            <svg viewBox="0 0 10 10">
              <style>
                /* g .a { fill: red } */
                g .a { fill: red }
                .a:hover { fill: red }
                @media print { .a { fill: red } }
                @import url(x.css);
                .a { fill: blue }
              </style>
              <rect class="a" width="10" height="10"/>
            </svg>
            """);
        Assert.Equal(0xFF0000FFu, fill);
    }

    [Fact]
    public void ImportantIsIgnoredRatherThanSpoilingTheValue()
    {
        var fill = FillOf("""
            <svg viewBox="0 0 10 10">
              <style>.a { fill: blue !important }</style>
              <rect class="a" width="10" height="10"/>
            </svg>
            """);
        Assert.Equal(0xFF0000FFu, fill);
    }

    [Fact]
    public void CdataStyleContentIsRead()
    {
        var fill = FillOf("""
            <svg viewBox="0 0 10 10">
              <style><![CDATA[ .a > .b { fill: red } .a { fill: blue } ]]></style>
              <rect class="a" width="10" height="10"/>
            </svg>
            """);
        Assert.Equal(0xFF0000FFu, fill);
    }
}
