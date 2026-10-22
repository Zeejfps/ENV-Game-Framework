using ZGF.Fonts;

namespace ZGF.Gui.Hosting;

internal static class GuiFonts
{
    private const int DefaultPixels = 16;

    /// <summary>The pixel size the default 16-point text is baked at on a <paramref name="dpiScale"/> canvas.</summary>
    public static int PixelSize(float dpiScale) => Math.Max(1, (int)MathF.Round(DefaultPixels * dpiScale));

    public static FontHandle LoadDefault(FreeTypeFontBackend fonts, float dpiScale) =>
        fonts.LoadFontFromMemory(EmbeddedAssets.LoadFontBytes("Inter-Regular.ttf"), PixelSize(dpiScale));
}
