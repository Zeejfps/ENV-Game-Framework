using System.Text.Json;

namespace MsdfBmpFont;

public sealed class MsdfBmpFontFileLoader
{
    public MsdfFontFile LoadFromFilePath(string pathToFile)
    {
        var appPath = AppContext.BaseDirectory;
        var fullFilePath = Path.Combine(appPath, pathToFile);
        var fileContents = File.ReadAllText(fullFilePath);
        var fontData = JsonSerializer.Deserialize(fileContents, MsdfBmpFontJsonSerializationContext.Default.MsdfFontFile);
        if (fontData == null)
            throw new MsdfBmpFontLoadingException("Failed to deserialize font data");
        return fontData;
    }

    public async Task<MsdfFontFile> LoadFromFilePathAsync(string pathToFile)
    {
        var appPath = AppContext.BaseDirectory;
        var fullFilePath = Path.Combine(appPath, pathToFile);
        var fileContents = await File.ReadAllTextAsync(fullFilePath);
        var fontData = JsonSerializer.Deserialize(fileContents, MsdfBmpFontJsonSerializationContext.Default.MsdfFontFile);
        if (fontData == null)
            throw new MsdfBmpFontLoadingException("Failed to deserialize font data");
        return fontData;
    }
}