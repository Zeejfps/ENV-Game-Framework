using System.Reflection;
using ZGF.AppUtils;

namespace ZGF.ILSpy;

/// <summary>The themes that ship inside the app, read once from its embedded theme files: the default
/// dark and light ones first, then the other dark ones and the other light ones, each by id.</summary>
public static class BuiltInThemes
{
    private const string ResourcePrefix = "Themes/";
    private const string SchemaResource = "theme.schema.json";
    private const string GuideResource = "themes-readme.md";
    private const int DarkRank = 0;
    private const int LightRank = 1;
    private const int OtherDarkRank = 2;
    private const int OtherLightRank = 3;

    private static readonly Assembly Resources = typeof(BuiltInThemes).Assembly;

    public static IReadOnlyList<ThemeFile> Files { get; } = ReadFiles();

    private static readonly ThemeCatalog Catalog = new(Files);

    /// <summary>The default dark theme, whose code and icon colors the screen starts with until the app
    /// hands it the chosen theme.</summary>
    public static AppTheme Dark { get; } = Catalog.Find(ThemeCatalog.DarkId);

    public static AppTheme Light { get; } = Catalog.Find(ThemeCatalog.LightId);

    /// <summary>What was wrong in the built-in theme files; nothing, unless one was broken by an edit.</summary>
    public static IReadOnlyList<ThemeError> Errors => Catalog.Errors;

    /// <summary>The JSON schema theme files name in <c>$schema</c>, for editors to complete and check their keys.</summary>
    public static string ReadSchema() => EmbeddedAssets.LoadString(Resources, SchemaResource);

    /// <summary>The README the themes folder holds, on how to write a theme.</summary>
    public static string ReadGuide() => EmbeddedAssets.LoadString(Resources, GuideResource);

    private static List<ThemeFile> ReadFiles()
    {
        return Resources.GetManifestResourceNames()
            .Where(IsThemeResource)
            .Select(ReadFile)
            .OrderBy(RankOf)
            .ThenBy(static f => f.Id.Text, StringComparer.Ordinal)
            .ToList();
    }

    private static bool IsThemeResource(string name) => name.StartsWith(ResourcePrefix, StringComparison.Ordinal);

    private static ThemeFile ReadFile(string resource)
    {
        var id = ThemeId<ThemeFile>.Of(Path.GetFileNameWithoutExtension(resource));
        var json = EmbeddedAssets.LoadBytes(Resources, resource);
        return ThemeFile.Read(id, resource, json);
    }

    private static int RankOf(ThemeFile file)
    {
        if (file.Id.Equals(ThemeCatalog.DarkId))
            return DarkRank;
        if (file.Id.Equals(ThemeCatalog.LightId))
            return LightRank;
        if (file.Type == ThemeType.Light)
            return OtherLightRank;
        return OtherDarkRank;
    }
}
