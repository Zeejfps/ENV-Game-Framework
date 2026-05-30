using System.Reflection;

namespace ZGF.Gui;

internal static class EmbeddedAssets
{
    private static readonly Assembly Assembly = typeof(EmbeddedAssets).Assembly;

    public static string LoadShaderSource(string fileName)
    {
        using var stream = Assembly.GetManifestResourceStream(fileName)
            ?? throw new InvalidOperationException(
                $"Embedded shader '{fileName}' not found. Available resources: " +
                string.Join(", ", Assembly.GetManifestResourceNames()));
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
