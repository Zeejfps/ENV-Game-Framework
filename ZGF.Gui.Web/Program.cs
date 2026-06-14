using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace ZGF.Gui.Web;

// .NET WASM browser-app entry. Main runs once at startup; the [JSExport] methods
// below are invoked from main.js. See ZGF.Gui.Web.csproj for the model rationale.
[SupportedOSPlatform("browser")]
public static partial class Program
{
    public static void Main()
    {
        // Nothing to do here yet — the JS side calls the exports below once the
        // runtime is up. A real host will construct the canvas + render loop here.
        Console.WriteLine("ZGF.Gui.Web runtime started.");
    }

    /// Runs the font validation spike (FreeType rasterization + HarfBuzz shaping
    /// under browser-wasm) and returns a human-readable summary for the page.
    /// This is the deliverable that proves the NativeFileReference + AOT + emsdk
    /// pipeline works end to end (docs/web-font-rendering.md §7).
    [JSExport]
    internal static string RunFontSpike() => FontSpike.Run();

    /// Per-frame seam for the render loop, driven by requestAnimationFrame in
    /// main.js. Today it's a no-op; the WebGL2 canvas backend will plug its
    /// BeginFrame/drawContent/EndFrame in here (separate plan).
    [JSExport]
    internal static void Tick(double timestampMs)
    {
        _ = timestampMs;
    }
}
