using ZGF.Fonts;
using ZGF.Geometry;

namespace ZGF.Gui.Tests;

/// <summary>
/// The fixed-advance cell path: every column lands on its own pitch, nothing is shaped, and the
/// decorations come from the font. Real FreeType and the real staging pipeline throughout — the
/// bugs this path exists to prevent (accumulated rounding, a thrashed shape cache) are only
/// observable against the actual glyph placement.
/// </summary>
public class GlyphRunTests
{
    private const float Advance = 10f;
    private const float RunTop = 100f;

    private static string FontPath => Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf");

    private static TextStyle Style => new() { TextColor = new StyleValue<uint>(0xFFFFFFFFu, true) };

    private static int[] CodePoints(string text) => text.EnumerateRunes().Select(r => r.Value).ToArray();

    private sealed class Fixture : IDisposable
    {
        public readonly FreeTypeFontBackend Fonts = new();
        public readonly FontHandle Font;
        public readonly CaptureCanvas Canvas;

        public Fixture(float dpiScale = 1f)
        {
            Font = Fonts.LoadFontFromFile(FontPath, (int)MathF.Round(16 * dpiScale));
            Canvas = new CaptureCanvas(Fonts, Font, dpiScale);
        }

        public void Dispose() => Fonts.Dispose();
    }

    private static void DrawRun(ICanvas canvas, string text, float left = 0f, float top = RunTop,
        float advance = Advance, bool underline = false, bool strikeThrough = false)
    {
        canvas.DrawGlyphRun(new DrawGlyphRunInputs
        {
            Origin = new PointF(left, top),
            CodePoints = CodePoints(text),
            CellAdvance = advance,
            Style = Style,
            ZIndex = 0,
            Underline = underline,
            StrikeThrough = strikeThrough,
        });
    }

    private static float[] GlyphLefts(CaptureCanvas canvas) =>
        canvas.Glyphs.Select(g => g.Rect.X).OrderBy(x => x).ToArray();

    // Each glyph is compared against the same glyph drawn alone on the same column rather than
    // against its neighbour: a glyph's quad carries its own bitmap bearing, so differencing two
    // different characters measures the bearings as much as the pitch.
    private static void AssertColumnsMatchSingletons(string text, float dpiScale = 1f)
    {
        using var run = new Fixture(dpiScale);
        run.Canvas.Frame(c => DrawRun(c, text));

        using var singly = new Fixture(dpiScale);
        singly.Canvas.Frame(c =>
        {
            var column = 0;
            foreach (var rune in text.EnumerateRunes())
                DrawRun(c, rune.ToString(), left: column++ * Advance);
        });

        Assert.Equal(GlyphLefts(singly.Canvas), GlyphLefts(run.Canvas));
    }

    [Fact]
    public void EveryColumn_LandsWhereItWouldAlone_WhateverItsNeighboursAreWorth()
    {
        // "iWiW" in a proportional font: the natural advances differ by more than twice, so a run
        // that accumulated them instead of the cell pitch would put nothing where it belongs.
        AssertColumnsMatchSingletons("iWiW");
    }

    [Fact]
    public void ARunIsNotKerned_SoAPairSitsWhereTheGridPutIt()
    {
        // "AV" kerns tightly in almost every proportional font. On the cell path it must not.
        AssertColumnsMatchSingletons("AV");
    }

    [Fact]
    public void AtAFractionalDpiScale_ColumnsDoNotDriftAcrossTheRow()
    {
        using var f = new Fixture(dpiScale: 1.25f);
        var metrics = f.Canvas.MeasureCellSize(Style);

        const int columns = 100;
        f.Canvas.Frame(c => DrawRun(c, new string('W', columns), advance: metrics.Advance));

        var lefts = GlyphLefts(f.Canvas);
        Assert.Equal(columns, lefts.Length);

        // Exactly N pitches from the first column to the last, with no accumulated rounding: the
        // per-column error a naive implementation leaks is what stops box drawing joining by
        // column 100.
        var pitch = lefts[1] - lefts[0];
        Assert.Equal(lefts[0] + pitch * (columns - 1), lefts[^1], 3);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(2f)]
    public void MeasureCellSize_IsAWholeNumberOfDevicePixels(float dpiScale)
    {
        using var f = new Fixture(dpiScale);

        var metrics = f.Canvas.MeasureCellSize(Style);

        Assert.Equal(MathF.Round(metrics.Advance * dpiScale), metrics.Advance * dpiScale, 3);
        Assert.Equal(MathF.Round(metrics.Height * dpiScale), metrics.Height * dpiScale, 3);
        Assert.True(metrics.Advance > 0f);
        Assert.True(metrics.Height > 0f);
    }

    [Fact]
    public void ColumnsLandWhereMeasureCellSizeSaysTheyWill()
    {
        using var f = new Fixture(dpiScale: 1.25f);
        var metrics = f.Canvas.MeasureCellSize(Style);

        f.Canvas.Frame(c =>
        {
            DrawRun(c, "W", left: 0f, advance: metrics.Advance);
            DrawRun(c, "W", left: metrics.Advance * 7f, advance: metrics.Advance);
        });

        var lefts = GlyphLefts(f.Canvas);
        Assert.Equal(2, lefts.Length);
        Assert.Equal(metrics.Advance * 7f, lefts[1] - lefts[0], 3);
    }

    [Fact]
    public void ABlankColumn_DrawsNothingButStillConsumesItsCell()
    {
        using var f = new Fixture();
        f.Canvas.Frame(c => DrawRun(c, "a b"));

        Assert.Equal(2, f.Canvas.Glyphs.Count);
        AssertColumnsMatchSingletons("a b");
    }

    [Fact]
    public void ControlCharacters_AreNotDrawn()
    {
        using var f = new Fixture();

        f.Canvas.Frame(c => c.DrawGlyphRun(new DrawGlyphRunInputs
        {
            Origin = new PointF(0f, RunTop),
            CodePoints = [0x00, 0x1B, 0x7F, 0x9B, 'a'],
            CellAdvance = Advance,
            Style = Style,
            ZIndex = 0,
        }));

        Assert.Single(f.Canvas.Glyphs);
    }

    [Fact]
    public void AnEmptyRun_DrawsNothing()
    {
        using var f = new Fixture();

        f.Canvas.Frame(c => c.DrawGlyphRun(new DrawGlyphRunInputs
        {
            Origin = new PointF(0f, RunTop),
            CodePoints = [],
            CellAdvance = Advance,
            Style = Style,
            ZIndex = 0,
        }));

        Assert.Empty(f.Canvas.Glyphs);
        Assert.Empty(f.Canvas.Rects);
    }

    [Fact]
    public void AnUndecoratedRun_StagesNoRects()
    {
        using var f = new Fixture();

        f.Canvas.Frame(c => DrawRun(c, "abc"));

        Assert.Empty(f.Canvas.Rects);
    }

    [Fact]
    public void Underline_SpansTheRunBelowTheBaseline()
    {
        using var f = new Fixture();
        var baseline = RunTop - f.Fonts.GetMetrics(f.Font).Ascender;

        f.Canvas.Frame(c => DrawRun(c, "abcd", underline: true));

        var rect = Assert.Single(f.Canvas.Rects);
        Assert.Equal(0f, rect.Rect.X, 1);
        Assert.Equal(Advance * 4f, rect.Rect.Z, 1);
        Assert.True(rect.Rect.Y < baseline,
            $"underline at {rect.Rect.Y} should sit below the baseline at {baseline}");
        Assert.True(rect.Rect.W >= 1f);
    }

    [Fact]
    public void StrikeThrough_CrossesTheRunAboveTheBaseline()
    {
        using var f = new Fixture();
        var baseline = RunTop - f.Fonts.GetMetrics(f.Font).Ascender;

        f.Canvas.Frame(c => DrawRun(c, "abcd", strikeThrough: true));

        var rect = Assert.Single(f.Canvas.Rects);
        Assert.True(rect.Rect.Y > baseline,
            $"strikethrough at {rect.Rect.Y} should sit above the baseline at {baseline}");
    }

    [Fact]
    public void BothDecorations_AreDrawnAtDifferentHeights()
    {
        using var f = new Fixture();

        f.Canvas.Frame(c => DrawRun(c, "abcd", underline: true, strikeThrough: true));

        Assert.Equal(2, f.Canvas.Rects.Count);
        Assert.NotEqual(f.Canvas.Rects[0].Rect.Y, f.Canvas.Rects[1].Rect.Y);
    }

    [Fact]
    public void DrawingACellGrid_LeavesTheShapeCacheAlone()
    {
        using var f = new Fixture();

        // Move the probe first with text that really is shaped, so a zero difference below reads as
        // "not shaped" rather than "nothing was measured".
        f.Canvas.MeasureTextWidth("shaped once", Style);
        var afterShaping = f.Fonts.ShapedRunCacheCount(f.Font);
        Assert.True(afterShaping > 0);

        f.Canvas.Frame(c =>
        {
            for (var row = 0; row < 40; row++)
                DrawRun(c, $"row {row} of a terminal repainting every frame", top: row * 20f);
        });

        Assert.Equal(afterShaping, f.Fonts.ShapedRunCacheCount(f.Font));
    }
}
