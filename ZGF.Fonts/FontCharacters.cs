using FreeTypeSharp;
using static FreeTypeSharp.FT;

namespace ZGF.Fonts;

/// <summary>The characters a font has glyphs for, read from its data without loading it for drawing, so
/// a font can be checked before it is registered.</summary>
public sealed class FontCharacters
{
    private readonly HashSet<int> _codepoints;

    private FontCharacters(HashSet<int> codepoints)
    {
        _codepoints = codepoints;
    }

    /// <summary>Whether the font has a glyph for <paramref name="codepoint"/>.</summary>
    public bool Contains(int codepoint) => _codepoints.Contains(codepoint);

    /// <summary>The characters of the face <paramref name="faceIndex"/> of <paramref name="data"/>; null
    /// when FreeType cannot load it, as when the data is no font or a font cut short.</summary>
    public static unsafe FontCharacters? TryRead(byte[] data, int faceIndex = 0)
    {
        using var library = new FreeTypeLibrary();
        fixed (byte* bytes = data)
        {
            FT_FaceRec_* face = null;
            var error = FT_New_Memory_Face(library.Native, bytes, (IntPtr)data.Length, (IntPtr)faceIndex, &face);
            if (error != FT_Error.FT_Err_Ok)
                return null;
            try
            {
                return new FontCharacters(ListCodepoints(face));
            }
            finally
            {
                FT_Done_Face(face);
            }
        }
    }

    /// <summary>Every codepoint the face's character map gives a glyph.</summary>
    private static unsafe HashSet<int> ListCodepoints(FT_FaceRec_* face)
    {
        var codepoints = new HashSet<int>();
        uint glyph;
        var codepoint = FT_Get_First_Char(face, &glyph);
        while (glyph != 0)
        {
            codepoints.Add((int)codepoint);
            codepoint = FT_Get_Next_Char(face, codepoint, &glyph);
        }
        return codepoints;
    }
}
