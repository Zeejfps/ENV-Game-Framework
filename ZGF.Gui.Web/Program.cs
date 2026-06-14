using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using ZGF.Fonts;
using ZGF.Gui;
using ZGF.Gui.Web.Demo;
using ZGF.Gui.Web.Files;
using ZGF.Gui.Web.Input;
using ZGF.Gui.Web.Rendering;
using AppUtilsAssets = ZGF.AppUtils.EmbeddedAssets;
using static ZGF.Gui.Web.Rendering.Gl;

namespace ZGF.Gui.Web;

[SupportedOSPlatform("browser")]
public static partial class Program
{
    private static WebGl2RenderedCanvas? _canvas;
    private static Context? _context;
    private static DemoScreen? _screen;
    private static WebFilePicker? _picker;

    public static void Main()
    {
        Native.WasmFreeTypeResolver.Install();
        Console.WriteLine("ZGF.Gui.Web runtime started.");
    }

    [JSExport]
    internal static string RunFontSpike() => FontSpike.Run();

    /// Initializes WebGL2 on the canvas, builds the canvas + default font, then the
    /// full-screen demo view tree. Call once from main.js after the runtime is up.
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

        var fonts = new FreeTypeFontBackend();
        var fontBytes = AppUtilsAssets.LoadBytes(typeof(View).Assembly, "Inter-Regular.ttf");
        var defaultFont = fonts.LoadFontFromMemory(fontBytes, (int)Math.Round(16 * dpr));

        _canvas = new WebGl2RenderedCanvas(width, height, fonts, defaultFont, (float)dpr);
        _context = new Context { Canvas = _canvas };
        _screen = new DemoScreen(_context, width, height) { UploadRequested = () => _ = PickAsync() };
        _screen.Root.LayoutSelf();

        WebFileDrop.FilesDropped += (point, files) => _ = SummarizeAsync("Dropped", files);

        Console.WriteLine($"ZGF.Gui.Web demo ready: {width}x{height} @ {dpr:0.##}x");
    }

    [JSExport]
    internal static void Tick(double timestampMs)
    {
        if (_canvas is null || _screen is null) return;

        _screen.Sync();
        _screen.Root.LayoutSelf();

        Webgl2.ClearColor(0.055f, 0.063f, 0.078f, 1f);
        Webgl2.Clear(COLOR_BUFFER_BIT);

        _canvas.BeginFrame();
        _screen.Root.DrawSelf();
        _canvas.EndFrame();
    }

    [JSExport]
    internal static void Resize(int width, int height, double dpr)
    {
        if (_canvas is null || _screen is null) return;
        _canvas.UpdateDpiScale((float)dpr);
        _canvas.Resize(width, height);
        _screen.Root.Width = width;
        _screen.Root.Height = height;
    }

    // Invoked synchronously from the DOM 'click' handler in main.js so the file
    // picker opens inside the live user gesture (the browser blocks it otherwise).
    [JSExport]
    internal static void HandleClick(double x, double y) => _screen?.HandleClick((float)x, (float)y);

    private static async Task PickAsync()
    {
        if (_picker is null) return;
        var files = await _picker.PickFilesAsync(new FilePickOptions { Multiple = true });
        await SummarizeAsync("Picked", files);
    }

    private static async Task SummarizeAsync(string source, IReadOnlyList<PickedFile> files)
    {
        if (_screen is null) return;

        if (files.Count == 0)
        {
            _screen.SetOutput($"{source}: (none)");
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
        _screen.SetOutput(string.Join("\n", lines));
    }
}
