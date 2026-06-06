using System.Reflection;
using AppUtilsAssets = ZGF.AppUtils.EmbeddedAssets;

namespace ZGF.Gui;

// Fonts are embedded in this (platform-independent) assembly. Shader sources are
// backend-specific and embedded into the platform package (e.g. ZGF.Gui.Desktop's
// ShaderAssets). Exposed to platform packages via [InternalsVisibleTo].
internal static class EmbeddedAssets
{
    private static readonly Assembly Assembly = typeof(EmbeddedAssets).Assembly;

    public static byte[] LoadFontBytes(string fileName) => AppUtilsAssets.LoadBytes(Assembly, fileName);
}
