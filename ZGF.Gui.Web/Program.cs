using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Web.Input;
using ZGF.Gui.Web.Rendering;
using AppUtilsAssets = ZGF.AppUtils.EmbeddedAssets;
using static ZGF.Gui.Web.Rendering.Gl;

namespace ZGF.Gui.Web;

// .NET WASM browser-app entry. Main runs once; the [JSExport] methods are invoked
// from main.js. See ZGF.Gui.Web.csproj for the model rationale.
//
// STATUS: scaffolding — never compiled or run here. The font spike (RunFontSpike)
// and the WebGL2 render path (StartAsync/Tick) are first drafts; treat the first
// `dotnet run` as the actual validation. See docs/web-font-rendering.md.
[SupportedOSPlatform("browser")]
public static partial class Program
{
    private static WebGl2RenderedCanvas? _canvas;
    private static IGlyphSource? _fonts;

    public static void Main()
    {
        Console.WriteLine("ZGF.Gui.Web runtime started.");
    }

    /// Runs the font validation spike (FreeType + HarfBuzz under browser-wasm) and
    /// returns a human-readable summary for the page (docs §7).
    [JSExport]
    internal static string RunFontSpike() => FontSpike.Run();

    /// Initializes WebGL2 on the given canvas and builds the rendered canvas +
    /// default font. Call once from main.js after the runtime is up.
    [JSExport]
    internal static async Task StartAsync(string canvasSelector, int width, int height, double dpr)
    {
        if (!await Webgl2.InitAsync(canvasSelector))
        {
            Console.Error.WriteLine("WebGL2 unavailable on this canvas/browser.");
            return;
        }

        await WebClipboard.InitAsync();

        var fonts = new FreeTypeFontBackend();
        var fontBytes = AppUtilsAssets.LoadBytes(typeof(View).Assembly, "Inter-Regular.ttf");
        var defaultFont = fonts.LoadFontFromMemory(fontBytes, (int)Math.Round(16 * dpr));

        _fonts = fonts;
        _canvas = new WebGl2RenderedCanvas(width, height, fonts, defaultFont, (float)dpr);
        Console.WriteLine($"WebGL2 canvas ready: {width}x{height} @ {dpr:0.##}x");
    }

    /// Per-frame render, driven by requestAnimationFrame in main.js.
    [JSExport]
    internal static void Tick(double timestampMs)
    {
        if (_canvas is null) return;

        Webgl2.ClearColor(0.10f, 0.10f, 0.12f, 1f);
        Webgl2.Clear(COLOR_BUFFER_BIT);

        _canvas.BeginFrame();
        DrawDemo(_canvas);
        _canvas.EndFrame();
    }

    [JSExport]
    internal static void Resize(int width, int height, double dpr)
    {
        if (_canvas is null) return;
        _canvas.UpdateDpiScale((float)dpr);
        _canvas.Resize(width, height);
    }

    // A static demo exercising the rect / glyph(text) / shape pipelines end to end,
    // now reacting to the DOM input bridge (WebInput) so the whole DOM -> GUI-coords
    // -> hit-test -> redraw path is exercised. The WebGL2 backend has no view/layout
    // system wired yet; this draws directly against ICanvas.
    private static void DrawDemo(ICanvas c)
    {
        var button = new RectF(40, 60, 420, 180);

        // Hover/press feedback proves pointer coords land in the right place.
        var bg = 0xFF3A6EA5u;
        if (WebInput.IsOver(button))
            bg = WebInput.IsButtonDown(0) ? 0xFF1E456Bu : 0xFF4E86C5u;

        c.DrawRect(new DrawRectInputs
        {
            Position = button,
            Style = new RectStyle { BackgroundColor = bg },
            ZIndex = 0,
        });

        c.DrawText(new DrawTextInputs
        {
            Position = new RectF(60, 200, 380, 40),
            Text = "ZGF GUI — WebGL2 + FreeType",
            Style = new TextStyle { TextColor = new(0xFFFFFFFF, true), FontSize = new(22f, true) },
            ZIndex = 1,
        });

        c.DrawCircle(new DrawCircleInputs
        {
            Center = new PointF(560, 150),
            Radius = 60f,
            Color = 0xFFE0A030,
            ZIndex = 1,
        });

        c.DrawLine(new DrawLineInputs
        {
            Start = new PointF(40, 30),
            End = new PointF(620, 30),
            Thickness = 4f,
            Color = 0xFF66CC99,
            ZIndex = 0,
        });

        // Live cursor marker — visualizes the DOM -> GUI (Y-up) coordinate mapping.
        if (WebInput.MouseInside)
        {
            c.DrawCircle(new DrawCircleInputs
            {
                Center = WebInput.MousePoint,
                Radius = 6f,
                Color = 0xFFFF5555,
                ZIndex = 10,
            });
        }
    }
}
