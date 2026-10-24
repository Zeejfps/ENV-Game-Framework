using ZGF.Gui.Immediate;

namespace ZGF.ILSpy;

/// <summary>The faces code is drawn with, as <see cref="MonoFonts.Code"/>: the bundled one, and each
/// installed font chosen this session, read from its file once.</summary>
internal sealed class CodeFontFaces(FontFace bundled)
{
    private readonly Dictionary<CodeFont, FontFace> _read = new();

    public FontFace Bundled => bundled;

    /// <summary>The face of <paramref name="font"/>, the bundled one when it is null; null when its file
    /// cannot be read.</summary>
    public FontFace? FaceOf(CodeFont? font)
    {
        if (font == null)
            return bundled;
        if (_read.TryGetValue(font, out var face))
            return face;
        var data = ReadOrNull(font.Path);
        if (data == null)
            return null;
        face = new FontFace(MonoFonts.Code, data, FaceIndex: font.FaceIndex);
        _read[font] = face;
        return face;
    }

    private static byte[]? ReadOrNull(string path)
    {
        try
        {
            return File.ReadAllBytes(path);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
