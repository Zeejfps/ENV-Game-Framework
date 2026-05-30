using System.Reflection;
using AppUtilsAssets = ZGF.AppUtils.EmbeddedAssets;

namespace ZGF.Gui;

internal static class EmbeddedAssets
{
    private static readonly Assembly Assembly = typeof(EmbeddedAssets).Assembly;

    public static string LoadShaderSource(string fileName) => AppUtilsAssets.LoadString(Assembly, fileName);

    public static byte[] LoadFontBytes(string fileName) => AppUtilsAssets.LoadBytes(Assembly, fileName);
}
