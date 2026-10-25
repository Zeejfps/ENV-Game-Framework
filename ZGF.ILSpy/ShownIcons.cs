using ZGF.Gui.Immediate;

namespace ZGF.ILSpy;

/// <summary>The icons the screen was last drawn with and the fonts they were drawn in, so the fonts of
/// newly chosen or reloaded icons are registered once, those the icons no longer draw in are let go,
/// and the pictures of the icons they replace are let go once.</summary>
public sealed class ShownIcons
{
    private AppIcons? _icons;
    private IReadOnlyList<FontFace> _fonts = [];

    /// <summary>The families of the fonts the icons were drawn in before the last change that the icons
    /// shown since no longer draw in.</summary>
    public IReadOnlyList<string> DroppedFamilies { get; private set; } = [];

    /// <summary>Whether <paramref name="icons"/> differ from those the screen was last drawn with; records
    /// them and their <paramref name="fonts"/> when they do.</summary>
    public bool TakeChange(AppIcons icons, IReadOnlyList<FontFace> fonts)
    {
        if (ReferenceEquals(icons, _icons))
            return false;
        DroppedFamilies = ListDropped(_fonts, fonts);
        _icons = icons;
        _fonts = fonts;
        return true;
    }

    private static List<string> ListDropped(IReadOnlyList<FontFace> before, IReadOnlyList<FontFace> after)
    {
        var kept = after
            .Select(static f => f.Family)
            .ToHashSet();
        return before
            .Select(static f => f.Family)
            .Where(f => !kept.Contains(f))
            .Distinct()
            .ToList();
    }
}
