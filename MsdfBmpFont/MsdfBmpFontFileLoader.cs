using System.Text.Json;

namespace MsdfBmpFont;

public sealed class MsdfBmpFontFileLoader
{
    public MsdfFontFile LoadFromFilePath(string pathToFile)
    {
        var fileContents = File.ReadAllText(pathToFile);
        var fontData = JsonSerializer.Deserialize<MsdfFontFile>(fileContents);
        if (fontData == null)
            throw new MsdfBmpFontLoadingException("Failed to deserialize font data");
        return fontData;
    }

    public async Task<MsdfFontFile> LoadFromFilePathAsync(string pathToFile)
    {
        var fileContents = await File.ReadAllTextAsync(pathToFile);
        var fontData = JsonSerializer.Deserialize<MsdfFontFile>(fileContents);
        if (fontData == null)
            throw new MsdfBmpFontLoadingException("Failed to deserialize font data");
        return fontData;
    }
}