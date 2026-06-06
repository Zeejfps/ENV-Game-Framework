using System.Reflection;
using AppUtilsAssets = ZGF.AppUtils.EmbeddedAssets;

namespace ZGF.Gui;

// Canvas shader sources (GLSL for OpenGL, .gen.metal for Metal) are embedded into this
// desktop package and selected per-RID at build time (see ZGF.Gui.Desktop.csproj).
internal static class ShaderAssets
{
    private static readonly Assembly Assembly = typeof(ShaderAssets).Assembly;

    public static string LoadShaderSource(string fileName) => AppUtilsAssets.LoadString(Assembly, fileName);
}
