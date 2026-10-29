namespace ZGF.ILSpy.Core;

/// <summary>The absolute path of a file an <see cref="AssemblyList"/> knows: an assembly, a package, or an
/// assembly read out of a package, by its path through the package as if the package were a folder.
/// Compared as the file system compares names: ignoring case, except on Linux.</summary>
public sealed record AssemblyPath
{
    /// <summary>How the file system compares names.</summary>
    public static readonly StringComparison Comparison = OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    /// <summary>Compares paths as the file system compares names.</summary>
    public static readonly StringComparer Comparer = StringComparer.FromComparison(Comparison);

    private AssemblyPath(string text)
    {
        Text = text;
    }

    public string Text { get; }

    /// <summary>The file name with its extension.</summary>
    public string FileName => Path.GetFileName(Text);

    /// <summary>The path <paramref name="text"/> is, made absolute; or null when it is not fully qualified
    /// or holds what no path can.</summary>
    public static AssemblyPath? Parse(string text)
    {
        if (!Path.IsPathFullyQualified(text))
            return null;
        try
        {
            return new AssemblyPath(Path.GetFullPath(text));
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    /// <summary>The path <paramref name="text"/>, which the program made itself from paths it parsed and
    /// must be one.</summary>
    public static AssemblyPath Of(string text)
    {
        var path = Parse(text);
        if (path == null)
            throw new ArgumentException($"'{text}' is no absolute path.", nameof(text));
        return path;
    }

    /// <summary>The path <paramref name="file"/> was read from: every file the list reads is named by the
    /// path it was read from.</summary>
    internal static AssemblyPath Of(ICSharpCode.Decompiler.Metadata.MetadataFile file) => Of(file.FileName);

    /// <summary>The path of <paramref name="relative"/> under this one, as when this is a package.</summary>
    public AssemblyPath Combine(string relative) => Of(Path.Combine(Text, relative));

    /// <summary>Whether this path lies under <paramref name="folder"/>, compared as the file system does.</summary>
    public bool IsUnder(AssemblyPath folder) =>
        Text.Length > folder.Text.Length
        && Text[folder.Text.Length] == Path.DirectorySeparatorChar
        && Text.StartsWith(folder.Text, Comparison);

    public bool Equals(AssemblyPath? other)
    {
        if (other is null)
            return false;
        return Comparer.Equals(Text, other.Text);
    }

    public override int GetHashCode() => Comparer.GetHashCode(Text);

    public override string ToString() => Text;
}
