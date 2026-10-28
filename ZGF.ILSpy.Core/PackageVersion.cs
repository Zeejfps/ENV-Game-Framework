using System.Text.RegularExpressions;

namespace ZGF.ILSpy.Core;

/// <summary>A package's version as NuGet allows one: two to four numbers joined by <c>.</c>, then
/// perhaps a prerelease label after <c>-</c> and build metadata after <c>+</c>, each made of
/// letters, digits and <c>-</c> joined by <c>.</c>. It names a folder and a file, so it can never
/// step out of the folder it is filed in.</summary>
public sealed partial record PackageVersion
{
    private const char BuildMetadataSeparator = '+';

    private PackageVersion(string text) => Text = text;

    public string Text { get; }

    /// <summary>The version as packages are filed and served under it: in lower case, without its
    /// build metadata.</summary>
    public string Filed
    {
        get
        {
            var at = Text.IndexOf(BuildMetadataSeparator);
            var bare = at >= 0 ? Text.Substring(0, at) : Text;
            return bare.ToLowerInvariant();
        }
    }

    /// <summary>The version <paramref name="text"/> spells; null when it is no version NuGet allows.</summary>
    public static PackageVersion? Parse(string text)
    {
        if (!Pattern().IsMatch(text))
            return null;
        return new PackageVersion(text);
    }

    public override string ToString() => Text;

    [GeneratedRegex(@"^\d+(\.\d+){1,3}(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?(\+[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?\z")]
    private static partial Regex Pattern();
}
