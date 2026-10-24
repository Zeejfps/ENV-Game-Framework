using ZGF.Fonts;

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
        if (!OperatingSystem.IsWindows()) return;

        var names = Families.Value
            .Select(f => f.Name)
            .ToArray();
        var sorted = names
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(sorted, names);
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
}
