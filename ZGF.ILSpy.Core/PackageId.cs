using System.Text.RegularExpressions;

namespace ZGF.ILSpy.Core;

/// <summary>A package's id as NuGet allows one: runs of letters, digits and <c>_</c> joined by
/// <c>.</c>, <c>-</c> or <c>_</c>, at most 100 characters. It names a folder and a file, so it can
/// never step out of the folder it is filed in.</summary>
public sealed partial record PackageId
{
    private const int MaxLength = 100;

    private PackageId(string text) => Text = text;

    public string Text { get; }

    /// <summary>The id as packages are filed and served under it.</summary>
    public string Lower => Text.ToLowerInvariant();

    /// <summary>The id <paramref name="text"/> spells; null when it is no id NuGet allows.</summary>
    public static PackageId? Parse(string text)
    {
        if (text.Length > MaxLength || !Pattern().IsMatch(text))
            return null;
        return new PackageId(text);
    }

    public override string ToString() => Text;

    [GeneratedRegex(@"^\w+([_.-]\w+)*\z")]
    private static partial Regex Pattern();
}
