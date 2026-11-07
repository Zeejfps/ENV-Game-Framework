using ZGF.Fonts;
using ZGF.Fonts.SystemFonts;

namespace ZGF.Gui.Tests;

public class SystemFontsTests
{
    private static readonly Lazy<IReadOnlyList<SystemFontFamily>> Families = new(() => NativeSystemFonts.Create().ListFamilies());

    private static SystemFontFamily Find(string name) => Families.Value.Single(f => f.Name == name);

    [Fact]
    public void Windows_lists_consolas_as_a_monospaced_family_with_its_file()
    {
        if (!OperatingSystem.IsWindows()) return;

        var consolas = Find("Consolas");
        var regular = consolas.FindRegular();

        Assert.True(consolas.IsMonospaced);
        Assert.NotNull(regular);
        Assert.Equal("consola.ttf", Path.GetFileName(regular.File.Path), ignoreCase: true);
        Assert.True(File.Exists(regular.File.Path));
        Assert.Equal(SystemFontStyle.Normal, regular.Style);
    }

    [Fact]
    public void A_family_with_a_bold_face_finds_it()
    {
        if (!OperatingSystem.IsWindows()) return;

        var bold = Find("Consolas").FindBold();

        Assert.NotNull(bold);
        Assert.Equal(700, bold.Weight);
        Assert.Equal("consolab.ttf", Path.GetFileName(bold.File.Path), ignoreCase: true);
    }

    [Fact]
    public void A_proportional_family_is_not_monospaced()
    {
        if (!OperatingSystem.IsWindows()) return;

        Assert.False(Find("Segoe UI").IsMonospaced);
    }

    [Fact]
    public void A_face_in_a_collection_file_carries_its_index()
    {
        if (!OperatingSystem.IsWindows()) return;

        var math = Find("Cambria Math").FindRegular();

        Assert.NotNull(math);
        Assert.Equal("cambria.ttc", Path.GetFileName(math.File.Path), ignoreCase: true);
        Assert.Equal(1, math.File.FaceIndex);
    }

    [Fact]
    public void A_symbol_font_says_so()
    {
        if (!OperatingSystem.IsWindows()) return;

        Assert.True(Find("Wingdings").IsSymbol);
        Assert.False(Find("Consolas").IsSymbol);
    }

    [Fact]
    public void The_families_come_sorted_by_name()
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS()) return;

        var names = Families.Value
            .Select(f => f.Name)
            .ToArray();
        var sorted = names
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(sorted, names);
    }

    [Fact]
    public void A_font_with_bitmaps_for_small_sizes_draws_from_its_outlines()
    {
        if (!OperatingSystem.IsWindows()) return;

        var regular = Find("BatangChe").FindRegular()!;
        using var fonts = new FreeTypeFontBackend();
        var font = fonts.LoadFontFromFile(regular.File.Path, 20, regular.File.FaceIndex);

        Assert.True(fonts.TryGetGlyph(fonts.ResolveGlyph(font, 'T'), out var glyph));
        Assert.True(glyph.Width > 0);
    }

    [Fact]
    public void A_listed_face_loads_and_draws_its_letters()
    {
        if (!OperatingSystem.IsWindows()) return;

        var regular = Find("Consolas").FindRegular()!;
        using var fonts = new FreeTypeFontBackend();
        var font = fonts.LoadFontFromFile(regular.File.Path, 16, regular.File.FaceIndex);

        Assert.False(fonts.ResolveGlyph(font, 'M').IsMissing);
    }

    [Fact]
    public void Mac_lists_menlo_as_a_monospaced_family_with_its_file()
    {
        if (!OperatingSystem.IsMacOS()) return;

        var menlo = Find("Menlo");
        var regular = menlo.FindRegular();

        Assert.True(menlo.IsMonospaced);
        Assert.NotNull(regular);
        Assert.Equal("Menlo.ttc", Path.GetFileName(regular.File.Path));
        Assert.True(File.Exists(regular.File.Path));
        Assert.Equal(400, regular.Weight);
        Assert.Equal(SystemFontStyle.Normal, regular.Style);
    }

    [Fact]
    public void Mac_finds_a_bold_face_at_its_index_in_a_collection()
    {
        if (!OperatingSystem.IsMacOS()) return;

        var menlo = Find("Menlo");
        var bold = menlo.FindBold();

        Assert.NotNull(bold);
        Assert.Equal(700, bold.Weight);
        Assert.Equal(menlo.FindRegular()!.File.Path, bold.File.Path);
        Assert.NotEqual(menlo.FindRegular()!.File.FaceIndex, bold.File.FaceIndex);
        Assert.Equal(SystemFontStyle.Italic, menlo.FindFace(400, SystemFontStyle.Italic)!.Style);
    }

    [Fact]
    public void Mac_gives_a_variable_fonts_named_instance_its_instance_index()
    {
        if (!OperatingSystem.IsMacOS()) return;

        var bold = Find("Skia").FindBold()!;
        using var fonts = new FreeTypeFontBackend();
        var font = fonts.LoadFontFromFile(bold.File.Path, 16, bold.File.FaceIndex);

        Assert.NotEqual(0, bold.File.FaceIndex >> 16);
        Assert.False(fonts.ResolveGlyph(font, 'M').IsMissing);
    }

    [Fact]
    public void Mac_tells_proportional_and_symbol_families_apart()
    {
        if (!OperatingSystem.IsMacOS()) return;

        Assert.False(Find("Helvetica").IsMonospaced);
        Assert.True(Find("Wingdings").IsSymbol);
        Assert.False(Find("Menlo").IsSymbol);
    }

    [Fact]
    public void Mac_leaves_out_the_hidden_system_families()
    {
        if (!OperatingSystem.IsMacOS()) return;

        Assert.DoesNotContain(Families.Value, f => f.Name.StartsWith('.'));
    }

    [Fact]
    public void Mac_listed_face_loads_and_draws_its_letters()
    {
        if (!OperatingSystem.IsMacOS()) return;

        var bold = Find("Menlo").FindBold()!;
        using var fonts = new FreeTypeFontBackend();
        var font = fonts.LoadFontFromFile(bold.File.Path, 16, bold.File.FaceIndex);

        Assert.False(fonts.ResolveGlyph(font, 'M').IsMissing);
    }

    [Theory]
    [InlineData(-0.8, 100)]
    [InlineData(-0.4, 300)]
    [InlineData(0.0, 400)]
    [InlineData(0.1, 400)]
    [InlineData(0.23, 500)]
    [InlineData(0.4, 700)]
    [InlineData(1.0, 900)]
    public void CoreText_weights_map_to_the_nearest_standard_weight(double normalized, int weight)
    {
        Assert.Equal(weight, MacSystemFonts.ToWeight(normalized));
    }

    [Theory]
    [InlineData(-1.0, 1)]
    [InlineData(-0.2, 4)]
    [InlineData(0.0, 5)]
    [InlineData(0.2, 6)]
    [InlineData(1.0, 9)]
    public void CoreText_widths_map_to_stretches_from_1_to_9(double normalized, int stretch)
    {
        Assert.Equal(stretch, MacSystemFonts.ToStretch(normalized));
    }
}
