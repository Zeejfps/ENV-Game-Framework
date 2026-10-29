namespace ZGF.ILSpy;

/// <summary>The icon themes the app can be drawn with, by id: the built-in ones, then the user's. Every
/// theme can draw in the reference theme's fonts, and a key no theme in its chain draws is drawn as the
/// reference theme draws it. One that cannot be resolved falls back to its parent, else to the reference
/// theme.</summary>
public sealed class IconThemeCatalog : ThemeCatalog<IconThemeFile, IconTheme>
{
    public static readonly ThemeId<IconThemeFile> ReferenceId = ThemeId<IconThemeFile>.Of("lucide");

    /// <param name="builtIns">The built-in icon themes in the order a menu lists them, which must include
    /// <see cref="ReferenceId"/>, mapping every key.</param>
    public IconThemeCatalog(IReadOnlyList<IconThemeFile> builtIns) : base(builtIns)
    {
        LoadUserThemes([]);
    }

    protected override ThemeId<IconThemeFile> DefaultId => ReferenceId;

    protected override IconTheme Build(IconThemeFile file, IReadOnlyList<IconThemeFile> chain)
    {
        var merged = new MergedIconTheme();
        var isReference = IsReference(file);
        if (!isReference)
            merged.AddFonts(FileOf(ReferenceId));
        foreach (var link in chain)
            merged.Add(link);
        var fallback = isReference ? null : Resolve(ReferenceId);
        var errors = new List<ThemeError>();
        var theme = merged.Build(fallback, errors);
        AddErrors(errors);
        return theme;
    }

    private static bool IsReference(IconThemeFile file) => file.Id.Equals(ReferenceId);

    protected override ThemeId<IconThemeFile> FallbackIdOf(IconThemeFile file) => ReferenceId;
}
