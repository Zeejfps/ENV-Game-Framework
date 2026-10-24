using ZGF.Fonts;

namespace ZGF.ILSpy;

/// <summary>The fonts the display options offer to draw code in: the bundled one, then the installed
/// monospaced families once they are listed, which they are only once.</summary>
public sealed class CodeFontChoices
{
    public const string BundledName = "JetBrains Mono";

    private bool _listingAsked;

    /// <summary>The fonts in the order offered: null for the bundled one, then the installed ones.</summary>
    public IReadOnlyList<CodeFont?> Fonts { get; private set; } = [null];

    /// <summary>The name of each of <see cref="Fonts"/>.</summary>
    public string[] Labels { get; private set; } = [BundledName];

    public bool IsListed { get; private set; }

    /// <summary>True the first time only: the installed fonts should be listed.</summary>
    public bool TakeListingRequest()
    {
        if (_listingAsked)
            return false;
        _listingAsked = true;
        return true;
    }

    /// <summary>Offers the families of <paramref name="families"/> code can be drawn in after the bundled font.</summary>
    public void Keep(IReadOnlyList<SystemFontFamily> families)
    {
        var installed = families
            .Where(IsOffered)
            .Select(CodeFont.Of);
        Fonts = new CodeFont?[] { null }
            .Concat(installed)
            .ToArray();
        Labels = Fonts
            .Select(NameOf)
            .ToArray();
        IsListed = true;
    }

    /// <summary>The name <paramref name="font"/> is offered by: the bundled font's when it is null.</summary>
    public static string NameOf(CodeFont? font)
    {
        if (font == null)
            return BundledName;
        return font.Family;
    }

    /// <summary>Whether <paramref name="family"/> lines code up in columns and draws letters, and is not
    /// the bundled font installed again.</summary>
    private static bool IsOffered(SystemFontFamily family)
    {
        if (!family.IsMonospaced)
            return false;
        if (family.IsSymbol)
            return false;
        if (family.FindRegular() == null)
            return false;
        return family.Name != BundledName;
    }
}
