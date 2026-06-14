using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Web.Files;
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
    private static WebFilePicker? _picker;

    // The upload button, in GUI (Y-up) space. Shared by draw + click hit-test.
    private static readonly RectF UploadButton = new(40, 60, 420, 110);
    private static string _filesSummary = "Click the button or drop files here to upload.";

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
        await WebFilePicker.InitAsync();
        _picker = new WebFilePicker();
        WebFileDrop.FilesDropped += (_, files) => _ = SummarizeAsync("Dropped", files);

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

    // Invoked synchronously from the DOM 'click' handler in main.js so the file
    // picker opens inside the live user gesture (the browser blocks it otherwise).
    // Must trigger the picker before the first await — see WebFilePicker.
    [JSExport]
    internal static void HandleClick(double x, double y)
    {
        if (_picker is null) return;
        if (x < UploadButton.Left || x >= UploadButton.Right ||
            y < UploadButton.Bottom || y >= UploadButton.Top)
            return;

        // Fire-and-forget: PickFilesAsync triggers input.click() synchronously here
        // (gesture preserved), then completes later with the user's choice.
        _ = PickAsync();
    }

    private static async Task PickAsync()
    {
        var files = await _picker!.PickFilesAsync(new FilePickOptions { Multiple = true });
        await SummarizeAsync("Picked", files);
    }

    // Reads the first file's bytes too, proving the content path (OpenReadAsync),
    // and renders a summary line per file.
    private static async Task SummarizeAsync(string source, IReadOnlyList<PickedFile> files)
    {
        if (files.Count == 0)
        {
            _filesSummary = $"{source}: (none)";
            return;
        }

        var lines = new List<string> { $"{source} {files.Count} file(s):" };
        for (var i = 0; i < files.Count; i++)
        {
            var f = files[i];
            var line = $"  {f.Name}  ({f.Size} bytes, {(string.IsNullOrEmpty(f.ContentType) ? "?" : f.ContentType)})";
            if (i == 0)
            {
                try
                {
                    await using var stream = await f.OpenReadAsync();
                    line += $"  -> read {stream.Length} bytes";
                }
                catch (Exception ex)
                {
                    line += $"  -> read failed: {ex.Message}";
                }
            }
            lines.Add(line);
        }
        _filesSummary = string.Join("\n", lines);
    }

    // A static demo exercising the rect / glyph(text) / shape pipelines end to end,
    // now reacting to the DOM input bridge (WebInput) so the whole DOM -> GUI-coords
    // -> hit-test -> redraw path is exercised. The WebGL2 backend has no view/layout
    // system wired yet; this draws directly against ICanvas.
    private static void DrawDemo(ICanvas c)
    {
        // Upload button. Highlights on hover/press and on a file drag-over, proving
        // both the pointer bridge and the file drag path land in the right place.
        var dragOver = WebFileDrop.IsDragOver;
        var bg = 0xFF3A6EA5u;
        if (dragOver) bg = 0xFF2E8B57u;                 // green while dragging files over
        else if (WebInput.IsOver(UploadButton))
            bg = WebInput.IsButtonDown(0) ? 0xFF1E456Bu : 0xFF4E86C5u;

        c.DrawRect(new DrawRectInputs
        {
            Position = UploadButton,
            Style = new RectStyle { BackgroundColor = bg },
            ZIndex = 0,
        });

        c.DrawText(new DrawTextInputs
        {
            Position = new RectF(60, UploadButton.Bottom, 380, UploadButton.Height),
            Text = dragOver ? "Release to upload" : "Upload files (click or drop)",
            Style = new TextStyle
            {
                TextColor = new(0xFFFFFFFF, true),
                FontSize = new(20f, true),
                VerticalAlignment = new(TextAlignment.Center, true),
            },
            ZIndex = 1,
        });

        // Result of the last pick/drop, including the bytes actually read.
        c.DrawText(new DrawTextInputs
        {
            Position = new RectF(40, 180, 720, 200),
            Text = _filesSummary,
            Style = new TextStyle { TextColor = new(0xFFCBD5E1, true), FontSize = new(14f, true) },
            ZIndex = 1,
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
