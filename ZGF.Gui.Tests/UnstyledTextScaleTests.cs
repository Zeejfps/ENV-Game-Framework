using ZGF.Fonts;
using ZGF.Gui.Testing;

namespace ZGF.Gui.Tests;

/// <summary>
/// Text that names no size of its own follows the scale like everything else. It is the easiest case
/// to leave behind: it resolves to the font the canvas was built with, and that font is loaded once,
/// at one pixel size, before the user has picked a scale.
/// </summary>
public sealed class UnstyledTextScaleTests : IDisposable
{
    private const float LogicalFontSize = 16f;

    private static string FontPath => Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf");

    private readonly FreeTypeFontBackend _fonts = new();
    private readonly List<RasterCanvas> _canvases = new();

    public void Dispose() => _fonts.Dispose();

    // Built the way the app builds one: the default font is loaded at the canvas's own scale, which is
    // what lets the canvas recover the logical size it was authored at.
    private RasterCanvas CanvasAt(float dpiScale)
    {
        var font = _fonts.LoadFontFromFile(FontPath, (int)MathF.Round(LogicalFontSize * dpiScale));
        var canvas = new RasterCanvas(400, 200, _fonts, font, dpiScale, 0u);
        _canvases.Add(canvas);
        return canvas;
    }

    // The logical height is what layout sees, so it is the thing that must not move: the glyphs grow
    // in device pixels precisely so that it doesn't.
    [Theory]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(1.875f)]
    [InlineData(2f)]
    public void TextWithNoSizeOfItsOwnKeepsItsLogicalHeightAsTheScaleRises(float dpiScale)
    {
        var atOne = CanvasAt(1f).MeasureTextLineHeight(new TextStyle());
        var atScale = CanvasAt(dpiScale).MeasureTextLineHeight(new TextStyle());

        Assert.Equal(atOne, atScale, tolerance: 1f);
    }

    // The live path: the canvas is built before the user's scale is known and told about it after, so
    // a scale change must not restate what size the unstyled text was.
    [Theory]
    [InlineData(1.25f)]
    [InlineData(1.875f)]
    public void AScaleChangeAfterConstructionLeavesTheLogicalHeightAlone(float dpiScale)
    {
        var canvas = CanvasAt(1f);
        var before = canvas.MeasureTextLineHeight(new TextStyle());

        canvas.UpdateDpiScale(dpiScale);

        Assert.Equal(before, canvas.MeasureTextLineHeight(new TextStyle()), tolerance: 1f);
    }

    // A ternary whose other arm is an int — `header ? FontSize.Caption : default` — yields 0, not an
    // unset prop, and 0 used to fall through to the base font at its load-time pixel size: a fixed
    // number of device pixels, which shrinks against the layout as the scale rises.
    [Theory]
    [InlineData(1.25f)]
    [InlineData(1.875f)]
    public void AZeroSizeIsTreatedAsNoSizeRatherThanAsTheRawBaseFont(float dpiScale)
    {
        var canvas = CanvasAt(1f);
        canvas.UpdateDpiScale(dpiScale);

        var unset = canvas.MeasureTextLineHeight(new TextStyle());
        var zero = canvas.MeasureTextLineHeight(new TextStyle { FontSize = 0f });

        Assert.Equal(unset, zero, tolerance: 0.001f);
    }
}
