using System.Reflection;
using AppUtilsAssets = ZGF.AppUtils.EmbeddedAssets;

namespace ZGF.Gui.OpenGL;

// GLSL canvas shader sources are embedded into this assembly (see ZGF.Gui.OpenGL.csproj) and
// compiled from source at runtime. Embedded here rather than in the desktop package so the
// OpenGL backend mirrors the Metal backend's self-contained ZGF.Gui.Metal packaging.
internal static class ShaderAssets
{
    private static readonly Assembly Assembly = typeof(ShaderAssets).Assembly;

    public static string LoadShaderSource(string fileName) => AppUtilsAssets.LoadString(Assembly, fileName);
}
