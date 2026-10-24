namespace ZGF.Fonts;

/// <summary>Fallback <see cref="ISystemFonts"/> for platforms it cannot list yet: no fonts, so a
/// font picker offers only the fonts the app ships.</summary>
public sealed class NoSystemFonts : ISystemFonts
{
    public IReadOnlyList<SystemFontFamily> ListFamilies() => [];
}
