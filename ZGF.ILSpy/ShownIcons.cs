namespace ZGF.ILSpy;

/// <summary>The icons the screen was last drawn with, so the fonts of newly chosen or reloaded icons are
/// registered once, and the pictures of the icons they replace are let go once.</summary>
public sealed class ShownIcons
{
    private AppIcons? _icons;

    /// <summary>Whether <paramref name="icons"/> differ from those the screen was last drawn with; records
    /// them when they do.</summary>
    public bool TakeChange(AppIcons icons)
    {
        if (ReferenceEquals(icons, _icons))
            return false;
        _icons = icons;
        return true;
    }
}
