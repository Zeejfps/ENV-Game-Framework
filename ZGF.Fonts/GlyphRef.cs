namespace ZGF.Fonts;

/// <summary>
/// A code point resolved to a glyph without shaping: the font that actually carries it, and its
/// index in that font.
/// </summary>
/// <remarks>
/// The font travels with the index because a fallback chain can answer from a different face than
/// the one asked, and an index means nothing without the face it came from. Pairing them makes that
/// mismatch unrepresentable rather than a rule the caller has to remember.
/// </remarks>
public readonly record struct GlyphRef(FontHandle Font, uint GlyphIndex)
{
    /// <summary>True when nothing in the chain covers the code point, so this is .notdef.</summary>
    public bool IsMissing => GlyphIndex == 0;
}
