using System.Text.Json;

namespace MsdfBmpFont;

public sealed class MsdfBmpFontLoader
{
    public FontData LoadFromFile(string pathToFile)
    {
        var fileContents = File.ReadAllText(pathToFile);
        var fontData = JsonSerializer.Deserialize<FontData>(fileContents);
        if (fontData == null)
            throw new MsdfBmpFontLoadingException("Failed to deserialize font data");
        return fontData;
    }

    public async Task<FontData> LoadFromFileAsync(string pathToFile)
    {
        var fileContents = await File.ReadAllTextAsync(pathToFile);
        var fontData = JsonSerializer.Deserialize<FontData>(fileContents);
        if (fontData == null)
            throw new MsdfBmpFontLoadingException("Failed to deserialize font data");
        return fontData;
    }
}