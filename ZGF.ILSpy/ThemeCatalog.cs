namespace ZGF.ILSpy;

/// <summary>The color themes the app can be drawn in, by id: the built-in ones, then the user's. One that
/// cannot be resolved falls back to its parent, else to the built-in theme of its type.</summary>
public sealed class ThemeCatalog : ThemeCatalog<ThemeFile, AppTheme>
{
    public const string DarkId = "dark";
    public const string LightId = "light";

    /// <param name="builtIns">The built-in themes in the order a menu lists them, which must include
    /// <see cref="DarkId"/> and <see cref="LightId"/>.</param>
    public ThemeCatalog(IReadOnlyList<ThemeFile> builtIns) : base(builtIns)
    {
        LoadUserThemes([]);
    }

    protected override string DefaultId => DarkId;

    /// <summary>The theme the chain makes, merged over the code and icon colors of the built-in theme of
    /// its root's type, or null when it does not make a whole one.</summary>
    protected override AppTheme? Build(ThemeFile file, IReadOnlyList<ThemeFile> chain)
    {
        var root = chain[0];
        var implicitBase = FindImplicitBase(root);
        if (implicitBase == null)
            return null;
        var merged = new MergedTheme();
        merged.AddCodeAndIcons(implicitBase);
        foreach (var link in chain)
            merged.Add(link);
        var errors = new List<ThemeError>();
        var theme = merged.Build(file.File, errors);
        AddErrors(errors);
        return theme;
    }

    /// <summary>The built-in theme whose code and icon colors <paramref name="root"/>, which extends no
    /// other, builds on: the one of its type. Null after reporting that it has no type.</summary>
    private ThemeFile? FindImplicitBase(ThemeFile root)
    {
        if (root.Type == null)
        {
            AddError(new ThemeError(root.File, 0, ThemeProblem.NoType));
            return null;
        }
        return FileOf(IdOfType(root.Type.Value));
    }

    protected override string FallbackIdOf(ThemeFile file)
    {
        var type = file.Type ?? ThemeType.Dark;
        return IdOfType(type);
    }

    private static string IdOfType(ThemeType type) => type == ThemeType.Light ? LightId : DarkId;
}
