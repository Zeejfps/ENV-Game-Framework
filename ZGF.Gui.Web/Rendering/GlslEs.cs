using System.Text;

namespace ZGF.Gui.Web.Rendering;

/// <summary>
/// Rewrites the desktop canvas GLSL (<c>#version 410</c>) to GLSL ES 3.00 so the
/// exact same shader bodies run under WebGL2. The only differences that matter
/// are the version directive and the mandatory precision qualifiers in ES — every
/// language feature the canvas shaders use (uint attributes, <c>flat</c>
/// varyings, std140 UBOs, <c>fwidth</c>, bit ops) is core in GLSL ES 3.00.
///
/// Keeping one source of truth (the desktop .glsl files, embedded into this host)
/// avoids a forked copy drifting out of sync. See docs/web-font-rendering.md and
/// the WebGL2 backend notes.
/// </summary>
internal static class GlslEs
{
    private const string Header =
        "#version 300 es\n" +
        "precision highp float;\n" +
        "precision highp int;\n";

    /// <param name="desktopGlsl">A canvas shader whose first non-empty line is
    /// <c>#version 410</c>.</param>
    public static string Adapt(string desktopGlsl)
    {
        var sb = new StringBuilder(desktopGlsl.Length + Header.Length);
        sb.Append(Header);

        // Drop the original #version line; keep everything else verbatim.
        var replacedVersion = false;
        foreach (var line in desktopGlsl.Split('\n'))
        {
            if (!replacedVersion && line.TrimStart().StartsWith("#version", StringComparison.Ordinal))
            {
                replacedVersion = true;
                continue;
            }
            sb.Append(line).Append('\n');
        }

        return sb.ToString();
    }
}
