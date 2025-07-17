using PngSharp.Api;
using ZGF.BMFontModule;

namespace ZGF.Gui.Tests;

public sealed class BitmapFont
{
    public IDecodedPng Png { get; }
    public BMFontFile FontMetrics { get; }

    private readonly Dictionary<(int, int), FontKerning> _kerningPairs = new();
    
    public BitmapFont(IDecodedPng png, BMFontFile file)
    {
        Png = png;
        FontMetrics = file;

        foreach (var kerning in file.Kernings)
        {
            _kerningPairs.Add((kerning.First, kerning.Second), kerning);;
        }
    }
    
    public bool TryGetGlyphInfo(int codePoint, out FontChar o)
    {
        return FontMetrics.TryGetFontChar(codePoint, out o);
    }

    public bool TryGetKerningPair(int c1, int c2, out int xOffset)
    {
        if (_kerningPairs.TryGetValue((c1, c2), out var kerning))
        {
            xOffset = kerning.Amount;
            return true;
        }
        
        xOffset = 0;
        return false;
    }
    
    public static BitmapFont LoadFromFile(string path)
    {
        var fontFile = BMFontFileUtils.DeserializeFromXmlFile(path);
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var fontPngFilePath = Path.Combine(directory, fontFile.Pages[0].File);
        var fontPng = PngSharp.Api.Png.DecodeFromFile(fontPngFilePath);
        return new BitmapFont(fontPng, fontFile);
    }
}