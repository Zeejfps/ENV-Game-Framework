using ZGF.Fonts;
using ZGF.Gui;

namespace ZGF.Gui.Web.SmokeTest;

/// <summary>
///     Forces the compiler to resolve the public surface of the platform-independent
///     toolkit and the font backend under the <c>net10.0-browser</c> target. If any of
///     these assemblies (or something they pull in transitively — e.g. the FreeType /
///     HarfBuzz managed wrappers) is incompatible with browser-wasm, building this project
///     fails — which is exactly the signal this smoke-test exists to produce. Uses
///     <c>typeof</c> rather than construction so the probe makes no assumptions about
///     constructors and never invokes native code at compile time.
/// </summary>
public static class WebCompatibilityProbe
{
    public static readonly Type[] ReferencedTypes =
    [
        // GUI toolkit core (renderer seam, views, context). Carries no desktop windowing
        // dependency — that lives in ZGF.Gui.Desktop, which this never references.
        typeof(ICanvas),
        typeof(RenderedCanvasBase),
        typeof(View),
        typeof(Context),

        // Font backend the web host reuses verbatim. Compiling FreeTypeFontBackend under
        // net10.0-browser is what proves the FreeType/HarfBuzz managed wrappers resolve for
        // the browser target (their native libs are linked by the host; see the doc).
        typeof(IGlyphSource),
        typeof(FreeTypeFontBackend),
    ];
}
