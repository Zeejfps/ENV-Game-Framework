namespace ZGF.Fonts;

/// <summary>An installed font family: its <paramref name="Name"/>, in English when the font has one,
/// and its <paramref name="Faces"/>.</summary>
public sealed record SystemFontFamily(string Name, IReadOnlyList<SystemFontFace> Faces)
{
    private const int NormalWeight = 400;
    private const int BoldWeight = 700;

    /// <summary>True when every face draws each character the same width.</summary>
    public bool IsMonospaced => Faces.All(f => f.IsMonospaced);

    /// <summary>True when the family draws symbols in place of letters, as Wingdings does.</summary>
    public bool IsSymbol => Faces.Any(f => f.IsSymbol);

    /// <summary>The upright face of normal width nearest the normal weight, or null when the family
    /// has no upright face.</summary>
    public SystemFontFace? FindRegular() => FindFace(NormalWeight, SystemFontStyle.Normal);

    /// <summary>The upright face of normal width nearest bold, or null when the family has no upright
    /// face heavier than its regular one.</summary>
    public SystemFontFace? FindBold()
    {
        var bold = FindFace(BoldWeight, SystemFontStyle.Normal);
        var regular = FindRegular();
        if (bold == null || regular == null)
            return null;
        if (bold.Weight <= regular.Weight)
            return null;
        return bold;
    }

    /// <summary>The face of <paramref name="style"/> nearest <paramref name="weight"/>, preferring the
    /// normal width, or null when no face has that style.</summary>
    public SystemFontFace? FindFace(int weight, SystemFontStyle style)
    {
        return Faces
            .Where(f => f.Style == style)
            .OrderBy(f => Math.Abs(f.Stretch - SystemFontFace.NormalStretch))
            .ThenBy(f => Math.Abs(f.Weight - weight))
            .FirstOrDefault();
    }
}

/// <summary>One face of a <see cref="SystemFontFamily"/>, such as "Bold Italic": the face in
/// <paramref name="File"/> to load, its <paramref name="Weight"/> from 1 to 999 (400 normal, 700 bold),
/// its <paramref name="Stretch"/> from 1 (ultra-condensed) to 9 (ultra-expanded), and its slant.</summary>
public sealed record SystemFontFace(
    string Name,
    SystemFontFile File,
    int Weight,
    int Stretch,
    SystemFontStyle Style,
    bool IsMonospaced,
    bool IsSymbol)
{
    public const int NormalStretch = 5;
}

public enum SystemFontStyle
{
    Normal,
    Oblique,
    Italic,
}
