using System.Reflection;
using ZGF.AppUtils;

namespace ZGF.ILSpy;

/// <summary>The icon themes that ship inside the app, read once from its embedded icon theme files: the
/// reference <c>lucide</c> theme first, then the others by id.</summary>
public static class BuiltInIconThemes
{
    private const string ResourcePrefix = "IconThemes/";
    private const string SchemaResource = "icon-theme.schema.json";
    private const string GuideResource = "icon-themes-readme.md";

    private static readonly Assembly Resources = typeof(BuiltInIconThemes).Assembly;

    public static IReadOnlyList<IconThemeFile> Files { get; } = ReadFiles();

    private static readonly IconThemeCatalog Catalog = new(Files);

    /// <summary>The reference theme, which draws every key and which the screen starts with until the app
    /// hands it the chosen icon theme.</summary>
    public static IconTheme Reference { get; } = Catalog.Find(IconThemeCatalog.ReferenceId);

    /// <summary>What was wrong in the built-in icon theme files; nothing, unless one was broken by an edit.</summary>
    public static IReadOnlyList<ThemeError> Errors => Catalog.Errors;

    /// <summary>The JSON schema icon theme files name in <c>$schema</c>, for editors to complete and check their keys.</summary>
    public static string ReadSchema() => EmbeddedAssets.LoadString(Resources, SchemaResource);

    /// <summary>The README the icon themes folder holds, on how to write an icon theme.</summary>
    public static string ReadGuide() => EmbeddedAssets.LoadString(Resources, GuideResource);

    private static List<IconThemeFile> ReadFiles()
    {
        return Resources.GetManifestResourceNames()
            .Where(IsIconThemeResource)
            .Select(ReadFile)
            .OrderBy(static f => f.Id != IconThemeCatalog.ReferenceId)
            .ThenBy(static f => f.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static bool IsIconThemeResource(string name) => name.StartsWith(ResourcePrefix, StringComparison.Ordinal);

    private static IconThemeFile ReadFile(string resource)
    {
        var id = Path.GetFileNameWithoutExtension(resource);
        var json = EmbeddedAssets.LoadBytes(Resources, resource);
        return IconThemeFile.Read(id, resource, json, new EmbeddedIconAssets(Resources));
    }
}

/// <summary>The fonts and pictures of a built-in icon theme: resources embedded in the app, named by
/// their resource names. Each font is drawn as the family of its id.</summary>
internal sealed class EmbeddedIconAssets(Assembly resources) : IconAssets
{
    public override IconAsset Read(string path)
    {
        var names = resources.GetManifestResourceNames();
        if (!names.Contains(path))
            return IconAsset.Failed(ThemeProblem.AssetUnreadable, path);
        return new IconAsset(EmbeddedAssets.LoadBytes(resources, path));
    }

    public override string FamilyOf(string fontId, string path) => fontId;
}
