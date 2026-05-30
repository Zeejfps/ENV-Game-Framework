using System.Reflection;

namespace ZGF.AppUtils;

public static class EmbeddedAssets
{
    public static byte[] LoadBytes(Assembly assembly, string name)
    {
        using var stream = OpenOrThrow(assembly, name);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    public static string LoadString(Assembly assembly, string name)
    {
        using var stream = OpenOrThrow(assembly, name);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static Stream OpenOrThrow(Assembly assembly, string name)
    {
        return assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{name}' not found in {assembly.GetName().Name}. " +
                $"Available resources: {string.Join(", ", assembly.GetManifestResourceNames())}");
    }
}
