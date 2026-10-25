namespace ZGF.ILSpy;

/// <summary>A font or picture file an icon theme names: its bytes, or why it cannot be read.</summary>
public readonly record struct IconAsset(byte[]? Bytes, ThemeProblem Problem = ThemeProblem.AssetUnreadable, string Detail = "")
{
    public static IconAsset Failed(ThemeProblem problem, string detail) => new(null, problem, detail);
}

/// <summary>Where an icon theme file's fonts and pictures are read from, by the paths the file names
/// them by, and the font family each font is drawn as.</summary>
public abstract class IconAssets
{
    /// <summary>The file at <paramref name="path"/>, as the theme file writes it.</summary>
    public abstract IconAsset Read(string path);

    /// <summary>The family the font <paramref name="fontId"/>, read from <paramref name="path"/>, is drawn as,
    /// which no other font may be drawn as.</summary>
    public abstract string FamilyOf(string fontId, string path);
}

/// <summary>The fonts and pictures of a user's icon theme: files inside the folder the theme file is in,
/// named by paths relative to it. A path that is absolute or leaves the folder is refused, and one that
/// is no path at all is unreadable. Each font is drawn as a family named by its file.</summary>
public sealed class IconAssetFolder(string folder) : IconAssets
{
    private const string FamilyPrefix = "icon-font:";
    private const string ParentFolder = "..";

    private readonly string _root = Path.GetFullPath(folder);

    public override IconAsset Read(string path)
    {
        try
        {
            return ReadInside(path);
        }
        catch (Exception e) when (IsReadFailure(e))
        {
            return IconAsset.Failed(ThemeProblem.AssetUnreadable, $"{path}: {e.Message}");
        }
    }

    private IconAsset ReadInside(string path)
    {
        var full = Resolve(path);
        if (full == null)
            return IconAsset.Failed(ThemeProblem.AssetOutsideFolder, path);
        return new IconAsset(File.ReadAllBytes(full));
    }

    /// <summary>True for what reading a file can throw, a path with characters no path may hold included.</summary>
    private static bool IsReadFailure(Exception e) =>
        e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException;

    public override string FamilyOf(string fontId, string path) => FamilyPrefix + Resolve(path);

    /// <summary>The full path of <paramref name="path"/> inside the folder; null when it is absolute or
    /// leaves the folder.</summary>
    private string? Resolve(string path)
    {
        if (Path.IsPathRooted(path))
            return null;
        var full = Path.GetFullPath(Path.Combine(_root, path));
        if (!IsInside(full))
            return null;
        return full;
    }

    private bool IsInside(string full)
    {
        var relative = Path.GetRelativePath(_root, full);
        if (Path.IsPathRooted(relative))
            return false;
        if (relative == ParentFolder)
            return false;
        return !relative.StartsWith(ParentFolder + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }
}
