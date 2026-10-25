using ZGF.Fonts;

namespace ZGF.Gui.Tests;

/// <summary>
/// A full atlas must never silently drop a glyph: nothing is drawn, nothing is logged, and the gap
/// looks exactly like missing font coverage. Latin needs a few hundred glyphs so this was never hit;
/// CJK needs thousands, across sizes and weights.
/// </summary>
public class GlyphAtlasTests
{
    // 16x16 glyphs pad to 18x18: three per row, three rows — nine fit in a 64x64 atlas.
    private const int GlyphSize = 16;
    private const int FitsIn64Square = 9;

    private static bool Reserve(GlyphAtlas atlas) => atlas.TryReserve(GlyphSize, GlyphSize, out _, out _);

    [Fact]
    public void GrowsWhenFullInsteadOfDroppingTheGlyph()
    {
        var atlas = new GlyphAtlas(64, 64);

        for (var i = 0; i < FitsIn64Square; i++)
            Assert.True(Reserve(atlas));

        Assert.True(Reserve(atlas), "the atlas should have grown rather than refusing the glyph");
        Assert.True(atlas.Height > 64);
    }

    [Fact]
    public void SignalsExhaustionOnceItCannotGrowAnyFurther()
    {
        var atlas = new GlyphAtlas(64, 64, maxHeight: 64);
        var signals = 0;
        atlas.Exhausted += () => signals++;

        for (var i = 0; i < FitsIn64Square; i++)
            Assert.True(Reserve(atlas));

        Assert.False(Reserve(atlas));
        Assert.True(atlas.IsExhausted);
        Assert.Equal(1, signals);

        Assert.False(Reserve(atlas));
        Assert.Equal(1, signals); // one signal, not one per dropped glyph
    }

    [Fact]
    public void GrowthStopsAtTheMaximumHeight()
    {
        var atlas = new GlyphAtlas(64, 64, maxHeight: 128);

        while (Reserve(atlas))
        {
        }

        Assert.Equal(128, atlas.Height);
        Assert.True(atlas.IsExhausted);
    }

    [Fact]
    public void GrowthNeverHandsOutSpaceTwice()
    {
        var atlas = new GlyphAtlas(64, 64);

        var rects = new List<(int X, int Y)>();
        for (var i = 0; i < FitsIn64Square * 3; i++)
        {
            Assert.True(atlas.TryReserve(GlyphSize, GlyphSize, out var x, out var y));
            rects.Add((x, y));
        }

        foreach (var a in rects)
        foreach (var b in rects)
        {
            if (a == b) continue;
            var overlaps = a.X < b.X + GlyphSize && b.X < a.X + GlyphSize
                && a.Y < b.Y + GlyphSize && b.Y < a.Y + GlyphSize;
            Assert.False(overlaps, $"reservation {a} overlaps {b} — growth invalidated the skyline");
        }
    }

    [Fact]
    public void FreedSpaceIsTakenByTheNextGlyphThatFitsOnceItsQuarantineEnds()
    {
        var atlas = new GlyphAtlas(64, 64);
        Assert.True(atlas.TryReserve(10, 12, out var x, out var y));

        atlas.Free(x, y, 10, 12);
        for (var frame = 0; frame < GlyphAtlas.QuarantineFrames; frame++)
            atlas.AdvanceFrame();
        Assert.True(atlas.TryReserve(8, 8, out var againX, out var againY));

        Assert.Equal((x, y), (againX, againY));
    }

    [Fact]
    public void FreedSpaceIsNotTakenWhileAFrameDrawnBeforeTheReleaseMayStillSampleIt()
    {
        var atlas = new GlyphAtlas(64, 64);
        Assert.True(atlas.TryReserve(10, 12, out var x, out var y));

        atlas.Free(x, y, 10, 12);
        Assert.True(atlas.TryReserve(8, 8, out var firstX, out var firstY));
        for (var frame = 1; frame < GlyphAtlas.QuarantineFrames; frame++)
            atlas.AdvanceFrame();
        Assert.True(atlas.TryReserve(8, 8, out var laterX, out var laterY));

        Assert.NotEqual((x, y), (firstX, firstY));
        Assert.NotEqual((x, y), (laterX, laterY));
        Assert.Equal(1, atlas.QuarantinedCount);
        Assert.Equal(0, atlas.FreedCount);
    }
}
