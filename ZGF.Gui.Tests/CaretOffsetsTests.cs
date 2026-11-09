using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui.Testing;

namespace ZGF.Gui.Tests;

/// <summary>
/// Every caret offset of a line, measured in one pass, is what measuring each prefix gives; and measuring
/// or drawing a long line reads the shaped glyphs where they are kept, without a buffer of its own.
/// </summary>
public sealed class CaretOffsetsTests : IDisposable
{
    private static string InterPath => Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf");

    private readonly FreeTypeFontBackend _fonts = new();

    public void Dispose() => _fonts.Dispose();

    private CaptureCanvas CanvasAt(float dpiScale, bool arabicFallback = false)
    {
        var font = _fonts.LoadFontFromFile(InterPath, (int)MathF.Round(16f * dpiScale));
        if (arabicFallback)
            _fonts.RegisterFallbackFont(_fonts.LoadFontFromFile(ArabicFontPath()!, (int)MathF.Round(16f * dpiScale)));
        return new CaptureCanvas(_fonts, font, dpiScale);
    }

    internal static string? ArabicFontPath()
    {
        string[] candidates = OperatingSystem.IsMacOS()
            ? ["/System/Library/Fonts/Supplemental/GeezaPro.ttc", "/Library/Fonts/GeezaPro.ttc", "/System/Library/Fonts/Supplemental/Arial.ttf"]
            : OperatingSystem.IsWindows()
                ? [Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf")]
                : ["/usr/share/fonts/truetype/noto/NotoNaskhArabic-Regular.ttf", "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"];
        return candidates.FirstOrDefault(File.Exists);
    }

    private static void AssertOffsetsMatchPrefixes(ICanvas canvas, string text)
    {
        var style = new TextStyle();
        var offsets = new float[text.Length + 1];

        canvas.MeasureTextPrefixes(text, style, offsets);

        for (var k = 0; k <= text.Length; k++)
            Assert.Equal(canvas.MeasureTextPrefix(text, k, style), offsets[k], tolerance: 0.01f);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public void Each_offset_is_the_width_of_its_prefix(float dpiScale)
    {
        var canvas = CanvasAt(dpiScale);

        AssertOffsetsMatchPrefixes(canvas, "");
        AssertOffsetsMatchPrefixes(canvas, "Hello, world");
        AssertOffsetsMatchPrefixes(canvas, "café näive");
        AssertOffsetsMatchPrefixes(canvas, "a😀b😀c");
        AssertOffsetsMatchPrefixes(canvas, "fi ffl office");
        AssertOffsetsMatchPrefixes(canvas, string.Concat(Enumerable.Repeat("The quick brown fox. ", 20)));
    }

    [ArabicFontFact]
    public void Each_offset_is_the_width_of_its_prefix_across_a_bidi_line_with_a_fallback_font()
    {
        var canvas = CanvasAt(1f, arabicFallback: true);

        AssertOffsetsMatchPrefixes(canvas, "abc مرحبا def");
        AssertOffsetsMatchPrefixes(canvas, string.Concat(Enumerable.Repeat("left مرحبا بالعالم right ", 15)));
    }

    [Fact]
    public void Offsets_need_a_slot_for_each_caret_position()
    {
        ICanvas canvas = CanvasAt(1f);

        Assert.Throws<ArgumentException>(() => canvas.MeasureTextPrefixes("abc", new TextStyle(), new float[3]));
    }

    [Fact]
    public void Measuring_and_drawing_a_long_line_allocate_nothing_once_it_is_shaped()
    {
        var capture = CanvasAt(1f);
        ICanvas canvas = capture;
        var style = new TextStyle();
        var text = string.Concat(Enumerable.Repeat("The quick brown fox. ", 20));
        var offsets = new float[text.Length + 1];
        void MeasureAndDraw()
        {
            canvas.MeasureTextWidth(text, style);
            canvas.MeasureTextPrefix(text, 100, style);
            canvas.MeasureTextPrefixes(text, style, offsets);
            capture.BeginFrame();
            canvas.DrawText(new DrawTextInputs { Position = new RectF(0, 0, 400, 20), Text = text, Style = style, ZIndex = 0 });
            capture.EndFrame();
        }
        for (var i = 0; i < 3; i++)
            MeasureAndDraw();

        var before = GC.GetAllocatedBytesForCurrentThread();
        MeasureAndDraw();

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}

/// <summary>A fact xUnit's <see cref="FactAttribute"/> demands be a type: skipped, and reported so, on a
/// machine with no Arabic font for the fallback.</summary>
internal sealed class ArabicFontFactAttribute : FactAttribute
{
    public ArabicFontFactAttribute()
    {
        if (CaretOffsetsTests.ArabicFontPath() == null)
            Skip = "No Arabic font on this machine to fall back on.";
    }
}
