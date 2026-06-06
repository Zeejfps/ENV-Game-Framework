using System.Reflection;
using AppUtilsAssets = ZGF.AppUtils.EmbeddedAssets;

namespace ZGF.Gui.Metal;

// Metal canvas shader sources (.gen.metal) are embedded into this assembly (see
// ZGF.Gui.Metal.csproj) and compiled from source at runtime. Embedded here rather than in
// the desktop package so the iOS host can reuse the same backend without pulling in desktop.
internal static class ShaderAssets
{
    private static readonly Assembly Assembly = typeof(ShaderAssets).Assembly;

    public static string LoadShaderSource(string fileName) => AppUtilsAssets.LoadString(Assembly, fileName);
}
