using ZGF.Fonts;

namespace ZGF.ILSpy;

/// <summary>An installed font to draw code in: its family's <paramref name="Family"/> name and the file
/// of its regular face, kept so the font loads at startup without listing every installed one.</summary>
public sealed record CodeFont(string Family, string Path, int FaceIndex)
{
    /// <summary>The font of <paramref name="family"/>'s regular face, or null when it has none.</summary>
    public static CodeFont? Of(SystemFontFamily family)
    {
        var regular = family.FindRegular();
        if (regular == null)
            return null;
        return new CodeFont(family.Name, regular.File.Path, regular.File.FaceIndex);
    }
}
