using System.Globalization;
using ZGF.Fonts;

namespace ZGF.Gui.Tests;

public class SystemFallbackFontsTests
{
    private static string InterPath => Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf");

    [Theory]
    [InlineData(0x4E2D)]
    [InlineData(0x3042)]
    [InlineData(0xD55C)]
    [InlineData(0x0628)]
    [InlineData(0x2714)]
    public void The_system_fallbacks_cover_what_Inter_does_not(int codePoint)
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS()) return;

        using var fonts = new FreeTypeFontBackend();
        var inter = fonts.LoadFontFromFile(InterPath, 16);
        foreach (var font in SystemFallbackFonts.Find())
            fonts.RegisterFallbackFont(fonts.LoadFontFromFile(font.Path, 16, font.FaceIndex));

        Assert.False(fonts.ResolveGlyph(inter, codePoint).IsMissing);
    }

    [Theory]
    [InlineData("zh-CN", "msyh.ttc")]
    [InlineData("zh-TW", "msjh.ttc")]
    [InlineData("ja-JP", "YuGothR.ttc")]
    [InlineData("ko-KR", "malgun.ttf")]
    public void The_ui_cultures_cjk_family_comes_first(string culture, string expected)
    {
        if (!OperatingSystem.IsWindows()) return;

        var cjk = new[] { "msyh.ttc", "msjh.ttc", "YuGothR.ttc", "malgun.ttf" };
        var first = SystemFallbackFonts.Find(new CultureInfo(culture))
            .Select(f => Path.GetFileName(f.Path))
            .First(cjk.Contains);

        Assert.Equal(expected, first);
    }
}
