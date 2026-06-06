using ZGF.Gui;
using ZGF.Rendering.Metal;

namespace ZGF.Gui.IOS.SmokeTest;

/// <summary>
///     Forces the compiler to resolve the public surface of the platform-independent
///     toolkit and the neutral Metal layer under the <c>net10.0-ios</c> target. If any of
///     these assemblies (or something they pull in transitively) is incompatible with iOS,
///     building this project fails — which is exactly the signal this smoke-test exists to
///     produce. Uses <c>typeof</c> rather than construction so the probe makes no
///     assumptions about constructors.
/// </summary>
public static class IosCompatibilityProbe
{
    public static readonly Type[] ReferencedTypes =
    [
        // GUI toolkit core (views, context, the renderer seam). It carries no desktop
        // windowing dependency — that lives in ZGF.Gui.Desktop, which this never references.
        typeof(ICanvas),
        typeof(RenderedCanvasBase),
        typeof(View),
        typeof(Context),

        // Neutral Metal layer the iOS renderer will reuse.
        typeof(IMetalSurface),
        typeof(MetalSurfaceRenderer),
    ];
}
