namespace ZGF.ILSpy;

/// <summary>The font code was last drawn in, null for the bundled one, so a newly chosen font is
/// registered once.</summary>
public sealed class ShownCodeFont
{
    private CodeFont? _font;

    /// <summary>Whether <paramref name="font"/> differs from the one code was last drawn in; records it
    /// when it does.</summary>
    public bool TakeChange(CodeFont? font)
    {
        if (font == _font)
            return false;
        _font = font;
        return true;
    }
}
