using System.Diagnostics.CodeAnalysis;

namespace MsdfBmpFont;

public class MsdfBmpFontMetrics
{
    private readonly Dictionary<int, GlyphInfo> _glyphsByCodePoint = new();
    private readonly Dictionary<(int, int), KerningInfo> _kerningByCodePointPair = new();
    
    public MsdfBmpFontMetrics(MsdfFontFile fontFile)
    {
        foreach (var glyph in fontFile.Glyphs)
            _glyphsByCodePoint[glyph.Id] = glyph;

        foreach (var kerning in fontFile.Kernings)
            _kerningByCodePointPair[(kerning.First, kerning.Second)] = kerning;
    }

    public bool TryGetGlyphInfo(int codePoint, [NotNullWhen(true)] out GlyphInfo? glyphInfo)
    {
        return _glyphsByCodePoint.TryGetValue(codePoint, out glyphInfo);
    }

    public bool TryGetKerningInfo(int firstCodePoint, int secondCodePoint,
        [NotNullWhen(true)] out KerningInfo? kerningInfo)
    {
        var pair = (firstCodePoint, secondCodePoint);
        return _kerningByCodePointPair.TryGetValue(pair, out kerningInfo);
    }
}