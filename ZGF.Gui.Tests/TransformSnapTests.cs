using System.Numerics;
using ZGF.Fonts;
using ZGF.Geometry;

namespace ZGF.Gui.Tests;

/// <summary>
/// What the canvas draws under <see cref="ICanvas.PushTranslation"/> and <see cref="ICanvas.PushScale"/>
/// is snapped to the pixel grid where it sits at rest and only then moved and scaled, exactly. So a
/// moving or growing subtree keeps the shape it has at rest, edges that meet still meet, and it moves
/// by fractions of a pixel instead of a pixel at a time — while at identity nothing changes.
/// </summary>
public sealed class TransformSnapTests : IDisposable
{
    private const uint Blue = 0xFF0000FFu;
    private const uint Red = 0xFFFF0000u;
    private const string Picture = "picture";
    private const float Tolerance = 1e-3f;
    private const uint LineShape = 2;

    private static string FontPath => Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf");

    private readonly FreeTypeFontBackend _fonts = new();

    // One face per scale, so canvases compared with each other draw from the same glyphs in the atlas.
    private readonly Dictionary<float, FontHandle> _faces = new();

    public void Dispose() => _fonts.Dispose();

    private CaptureCanvas CaptureAt(float dpiScale)
    {
        if (!_faces.TryGetValue(dpiScale, out var font))
        {
            font = _fonts.LoadFontFromFile(FontPath, (int)MathF.Round(16f * dpiScale));
            _faces[dpiScale] = font;
        }
        var canvas = new CaptureCanvas(_fonts, font, dpiScale);
        canvas.ImageSizes[Picture] = new Size { Width = 64f, Height = 32f };
        return canvas;
    }

    /// <summary>A bit of everything the canvas stages: a bordered, rounded rect beside one sharing its
    /// edge, a shadow, text with an underline-free run, a picture, a line, a circle, all under a clip.</summary>
    private static void Scene(ICanvas c)
    {
        c.PushClip(new RectF(5.3f, 7.6f, 300f, 200f));
        c.DrawRect(new DrawRectInputs
        {
            Position = new RectF(10.3f, 20.6f, 40.45f, 30.2f),
            Style = new RectStyle
            {
                BackgroundColor = Blue,
                BorderSize = BorderSizeStyle.All(1f),
                BorderColor = BorderColorStyle.All(Red),
                BorderRadius = BorderRadiusStyle.All(4f),
            },
            ZIndex = 0,
        });
        c.DrawRect(new DrawRectInputs
        {
            Position = new RectF(50.75f, 20.6f, 20.1f, 30.2f),
            Style = new RectStyle { BackgroundColor = Red },
            ZIndex = 0,
        });
        c.DrawBoxShadow(new DrawBoxShadowInputs
        {
            Position = new RectF(80.2f, 30.4f, 50f, 40f),
            BorderRadius = BorderRadiusStyle.All(6f),
            Shadow = new BoxShadowStyle { OffsetY = 8f, Blur = 16f, Spread = -2f, Color = 0x80000000u },
            ZIndex = 0,
        });
        c.DrawText(new DrawTextInputs
        {
            Position = new RectF(12.4f, 90.7f, 200f, 24f),
            Text = "Snap me",
            Style = new TextStyle { TextColor = Blue },
            ZIndex = 0,
        });
        c.DrawImage(new DrawImageInputs
        {
            Position = new RectF(140.3f, 60.5f, 64f, 32f),
            ImageId = Picture,
            TintColor = 0xFFFFFFFFu,
            ZIndex = 0,
            Rotation = 0f,
            CornerRadius = new Vector4(6f, 6f, 0f, 2f),
        });
        c.DrawLine(new DrawLineInputs { Start = new PointF(20f, 150f), End = new PointF(90f, 170f), Thickness = 2f, Color = Red, ZIndex = 0 });
        c.DrawCircle(new DrawCircleInputs { Center = new PointF(150f, 150f), Radius = 10f, Thickness = 0f, Color = Blue, ZIndex = 0 });
        c.PopClip();
    }

    private static CaptureCanvas Drawn(CaptureCanvas canvas, Action<ICanvas> draw)
    {
        canvas.Frame(draw);
        return canvas;
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    public void AnIdentityTransformDrawsExactlyWhatNoTransformDoes(float dpiScale)
    {
        var plain = Drawn(CaptureAt(dpiScale), Scene);
        var identity = Drawn(CaptureAt(dpiScale), c =>
        {
            c.PushTranslation(0f, 0f);
            c.PushScale(1f, 1f, 37f, 41f);
            Scene(c);
            c.PopScale();
            c.PopTranslation();
        });

        Assert.Equal(plain.Rects, identity.Rects);
        Assert.Equal(plain.Glyphs, identity.Glyphs);
        Assert.Equal(plain.Images, identity.Images);
        Assert.Equal(plain.Shadows, identity.Shadows);
        Assert.Equal(plain.Shapes, identity.Shapes);
        Assert.Equal(plain.Clips, identity.Clips);
    }

    [Theory]
    [InlineData(1f, 0.37f, -0.21f)]
    [InlineData(1.25f, 0.5f, 0.5f)]
    [InlineData(1.5f, -2.3f, 4.1f)]
    public void ATranslationMovesEveryDrawnEdgeByExactlyThatMuch(float dpiScale, float dx, float dy)
    {
        var rest = Drawn(CaptureAt(dpiScale), Scene);
        var moved = Drawn(CaptureAt(dpiScale), c =>
        {
            c.PushTranslation(dx, dy);
            Scene(c);
            c.PopTranslation();
        });

        var shift = new Vector4(dx, dy, 0f, 0f);
        AssertAll(rest.Rects, moved.Rects, (a, b) => AssertNear(a.Rect + shift, b.Rect));
        AssertAll(rest.Rects, moved.Rects, (a, b) => Assert.Equal(a.BorderSize, b.BorderSize));
        AssertAll(rest.Glyphs, moved.Glyphs, (a, b) => AssertNear(a.Rect + shift, b.Rect));
        AssertAll(rest.Glyphs, moved.Glyphs, (a, b) => Assert.Equal(a.AtlasUV, b.AtlasUV));
        AssertAll(rest.Images, moved.Images, (a, b) => AssertNear(a.Rect + shift, b.Rect));
        AssertAll(rest.Shadows, moved.Shadows, (a, b) => AssertNear(a.ShadowRect + shift, b.ShadowRect));
        // A line's data is its two ends; a circle's is its center and radius.
        AssertAll(rest.Shapes, moved.Shapes, (a, b) => AssertNear(
            a.ShapeData + (a.ShapeType == LineShape ? new Vector4(dx, dy, dx, dy) : new Vector4(dx, dy, 0f, 0f)), b.ShapeData));
        Assert.Equal(rest.Clips.Count, moved.Clips.Count);
        AssertNear(rest.Clips[1] + new Vector4(dx, dy, dx, dy), moved.Clips[1]);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    public void NeighboursStillShareAnEdgeWhileMoved(float dpiScale)
    {
        foreach (var dx in new[] { 0.1f, 0.33f, 0.5f, 0.77f, 1.2f })
        {
            var moved = Drawn(CaptureAt(dpiScale), c =>
            {
                c.PushTranslation(dx, 0f);
                Scene(c);
                c.PopTranslation();
            });

            var (left, right) = (moved.Rects[0].Rect, moved.Rects[1].Rect);
            Assert.Equal(left.X + left.Z, right.X, Tolerance);
        }
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    public void MovingSweepsSmoothlyRatherThanAPixelAtATime(float dpiScale)
    {
        var rest = Drawn(CaptureAt(dpiScale), Scene).Rects[0].Rect;
        for (var step = 0; step <= 60; step++)
        {
            var dy = step * 0.1f;
            var moved = Drawn(CaptureAt(dpiScale), c =>
            {
                c.PushTranslation(0f, dy);
                Scene(c);
                c.PopTranslation();
            }).Rects[0].Rect;

            Assert.Equal(rest.Y + dy, moved.Y, Tolerance);
            Assert.Equal(rest.W, moved.W, Tolerance);
        }
    }

    [Theory]
    [InlineData(1f, 1.04f)]
    [InlineData(1.25f, 1.1f)]
    [InlineData(1.5f, 0.9f)]
    public void AScaleMapsEverySnappedEdgeAboutItsPivot(float dpiScale, float scale)
    {
        var pivot = new Vector2(100f, 60f);
        var rest = Drawn(CaptureAt(dpiScale), Scene);
        var scaled = Drawn(CaptureAt(dpiScale), c =>
        {
            c.PushScale(scale, scale, pivot.X, pivot.Y);
            Scene(c);
            c.PopScale();
        });

        Vector4 About(Vector4 r) => new(pivot.X + (r.X - pivot.X) * scale, pivot.Y + (r.Y - pivot.Y) * scale, r.Z * scale, r.W * scale);
        AssertAll(rest.Rects, scaled.Rects, (a, b) => AssertNear(About(a.Rect), b.Rect));
        AssertAll(rest.Rects, scaled.Rects, (a, b) => AssertNear(a.BorderRadius * scale, b.BorderRadius));
        AssertAll(rest.Rects, scaled.Rects, (a, b) => AssertNear(a.BorderSize * scale, b.BorderSize));
        AssertAll(rest.Images, scaled.Images, (a, b) => AssertNear(About(a.Rect), b.Rect));
        AssertAll(rest.Images, scaled.Images, (a, b) => AssertNear(a.CornerRadius * scale, b.CornerRadius));
        AssertAll(rest.Shadows, scaled.Shadows, (a, b) => AssertNear(About(a.ShadowRect), b.ShadowRect));
        AssertAll(rest.Shadows, scaled.Shadows, (a, b) => Assert.Equal(a.Sigma * scale, b.Sigma, Tolerance));

        var (left, right) = (scaled.Rects[0].Rect, scaled.Rects[1].Rect);
        Assert.Equal(left.X + left.Z, right.X, Tolerance);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    public void ScaledTextIsDrawnFromGlyphsRasterizedAtTheScaledSize(float dpiScale)
    {
        const float scale = 1.25f;
        var rest = Drawn(CaptureAt(dpiScale), Scene);
        var scaled = Drawn(CaptureAt(dpiScale), c =>
        {
            c.PushScale(scale, scale, 0f, 0f);
            Scene(c);
            c.PopScale();
        });

        Assert.Equal(rest.Glyphs.Count, scaled.Glyphs.Count);
        var atlas = _fonts.AtlasWidth;
        for (var i = 0; i < rest.Glyphs.Count; i++)
        {
            // Its bitmap in the atlas is the scaled size, not the rest size stretched...
            var restBitmap = rest.Glyphs[i].AtlasUV.Z * atlas;
            var scaledBitmap = scaled.Glyphs[i].AtlasUV.Z * atlas;
            Assert.True(scaledBitmap > restBitmap, $"glyph {i} drawn from a {scaledBitmap}px bitmap, rest {restBitmap}px");
            // ...and drawn about as many device pixels wide as it has, so it is neither stretched nor squashed.
            Assert.Equal(scaledBitmap, scaled.Glyphs[i].Rect.Z * dpiScale, 1f);
        }

        // The run keeps its shape: it is the rest run, scaled.
        var restWidth = Span(rest.Glyphs);
        var scaledWidth = Span(scaled.Glyphs);
        Assert.Equal(restWidth * scale, scaledWidth, 1.5f);
    }

    [Fact]
    public void MovedTextIsDrawnFromTheGlyphsItHasAtRest()
    {
        var rest = Drawn(CaptureAt(1.25f), Scene);
        var moved = Drawn(CaptureAt(1.25f), c =>
        {
            c.PushTranslation(0.3f, 0.6f);
            Scene(c);
            c.PopTranslation();
        });

        AssertAll(rest.Glyphs, moved.Glyphs, (a, b) => Assert.Equal(a.AtlasUV, b.AtlasUV));
    }

    [Fact]
    public void AClipPushedInsideATransformMovesWithItAndOneOutsideDoesNot()
    {
        var outside = new RectF(0f, 0f, 400f, 300f);
        var inside = new RectF(10f, 10f, 100f, 50f);
        var rest = Drawn(CaptureAt(1f), c =>
        {
            c.PushClip(outside);
            c.PushClip(inside);
            c.DrawRect(new DrawRectInputs { Position = inside, Style = new RectStyle { BackgroundColor = Blue }, ZIndex = 0 });
            c.PopClip();
            c.PopClip();
        });
        var moved = Drawn(CaptureAt(1f), c =>
        {
            c.PushClip(outside);
            c.PushTranslation(0.5f, 0.25f);
            c.PushClip(inside);
            c.DrawRect(new DrawRectInputs { Position = inside, Style = new RectStyle { BackgroundColor = Blue }, ZIndex = 0 });
            c.PopClip();
            c.PopTranslation();
            c.PopClip();
        });

        Assert.Equal(rest.Clips[1], moved.Clips[1]);
        AssertNear(rest.Clips[2] + new Vector4(0.5f, 0.25f, 0.5f, 0.25f), moved.Clips[2]);
    }

    [Fact]
    public void NestedTransformsComposeAndPopBackToRest()
    {
        var rest = Drawn(CaptureAt(1.25f), Scene);
        var nested = Drawn(CaptureAt(1.25f), c =>
        {
            c.PushTranslation(0f, -6f);
            c.PushScale(1.04f, 1.04f, 50f, 40f);
            c.PopScale();
            c.PopTranslation();
            Scene(c);
        });
        var composed = Drawn(CaptureAt(1.25f), c =>
        {
            c.PushTranslation(0f, -6f);
            c.PushScale(1.04f, 1.04f, 50f, 40f);
            Scene(c);
            c.PopScale();
            c.PopTranslation();
        });

        Assert.Equal(rest.Rects, nested.Rects);
        var r = rest.Rects[1].Rect;
        AssertNear(new Vector4(50f + (r.X - 50f) * 1.04f, 40f + (r.Y - 40f) * 1.04f - 6f, r.Z * 1.04f, r.W * 1.04f),
            composed.Rects[1].Rect);
    }

    [Fact]
    public void ARectWhoseFadedEdgeMeetsAnImageIsNotBatchedAheadOfIt()
    {
        // Moved half a pixel, the image's right edge and the rect's left edge share the pixel between
        // them, each fading over it. Drawn after the image, the rect must stay after it; without its
        // faded pixel in its bounds, batching would put it with the earlier rect, ahead of the image.
        var canvas = Drawn(CaptureAt(1f), c =>
        {
            c.PushTranslation(0.5f, 0f);
            c.DrawRect(new DrawRectInputs { Position = new RectF(300f, 10f, 20f, 20f), Style = new RectStyle { BackgroundColor = Red }, ZIndex = 0 });
            c.DrawImage(new DrawImageInputs { Position = new RectF(10f, 10f, 64f, 32f), ImageId = Picture, TintColor = 0xFFFFFFFFu, ZIndex = 0, Rotation = 0f });
            c.DrawRect(new DrawRectInputs { Position = new RectF(74f, 10f, 20f, 32f), Style = new RectStyle { BackgroundColor = Blue }, ZIndex = 0 });
            c.PopTranslation();
        });

        Assert.Equal(
            [RenderedCanvasBase.DrawKind.Rect, RenderedCanvasBase.DrawKind.Image, RenderedCanvasBase.DrawKind.Rect],
            canvas.DrawCalls.Select(call => call.Kind));
    }

    [Fact]
    public void ARectWithEdgesOnTheGridIsBatchedAsBefore()
    {
        var canvas = Drawn(CaptureAt(1f), c =>
        {
            c.DrawRect(new DrawRectInputs { Position = new RectF(300f, 10f, 20f, 20f), Style = new RectStyle { BackgroundColor = Red }, ZIndex = 0 });
            c.DrawImage(new DrawImageInputs { Position = new RectF(10f, 10f, 64f, 32f), ImageId = Picture, TintColor = 0xFFFFFFFFu, ZIndex = 0, Rotation = 0f });
            c.DrawRect(new DrawRectInputs { Position = new RectF(74f, 10f, 20f, 32f), Style = new RectStyle { BackgroundColor = Blue }, ZIndex = 0 });
        });

        Assert.Equal([RenderedCanvasBase.DrawKind.Rect, RenderedCanvasBase.DrawKind.Image], canvas.DrawCalls.Select(call => call.Kind));
    }

    private static float Span(List<RenderedCanvasBase.GlyphInstance> glyphs)
    {
        var left = float.MaxValue;
        var right = float.MinValue;
        foreach (var glyph in glyphs)
        {
            left = MathF.Min(left, glyph.Rect.X);
            right = MathF.Max(right, glyph.Rect.X + glyph.Rect.Z);
        }
        return right - left;
    }

    private static void AssertAll<T>(List<T> expected, List<T> actual, Action<T, T> assert)
    {
        Assert.Equal(expected.Count, actual.Count);
        Assert.NotEmpty(expected);
        for (var i = 0; i < expected.Count; i++)
            assert(expected[i], actual[i]);
    }

    private static void AssertNear(Vector4 expected, Vector4 actual)
    {
        Assert.Equal(expected.X, actual.X, Tolerance);
        Assert.Equal(expected.Y, actual.Y, Tolerance);
        Assert.Equal(expected.Z, actual.Z, Tolerance);
        Assert.Equal(expected.W, actual.W, Tolerance);
    }
}
