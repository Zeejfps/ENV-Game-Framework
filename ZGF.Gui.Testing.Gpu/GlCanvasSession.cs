using ZGF.AppUtils;
using ZGF.Fonts;
using ZGF.Gui.OpenGL;

namespace ZGF.Gui.Testing.Gpu;

/// <summary>The shipping OpenGL canvas drawing into an FBO.</summary>
public sealed class GlCanvasSession : ICanvasSession
{
    private const int DefaultPixels = 16;

    private readonly GpuDevice _device;
    private readonly FreeTypeFontBackend _fonts;
    private readonly FontHandle _font;
    private readonly GlImageManager _images;
    private readonly GlSharedResources _shared;
    private readonly GlCanvasSurface _main;
    private readonly List<GlCanvasSurface> _surfaces = [];
    private bool _released;

    private GlCanvasSession(GpuDevice device, FreeTypeFontBackend fonts, FontHandle font, GlImageManager images,
        GlSharedResources shared, GlCanvasSurface main)
    {
        _device = device;
        _fonts = fonts;
        _font = font;
        _images = images;
        _shared = shared;
        _main = main;
    }

    public TargetSize Size => _main.Size;

    /// <inheritdoc/>
    public RenderedCanvasBase Canvas => _main.Canvas;

    public FreeTypeFontBackend Fonts => _fonts;

    /// <exception cref="ArgumentOutOfRangeException">The size is invalid or exceeds what the context can render to.</exception>
    /// <exception cref="InvalidOperationException">The device is not an OpenGL device.</exception>
    public static GlCanvasSession Create(GpuDevice device, int width, int height, float dpiScale = 1f)
    {
        device.RequireOpenGl();
        var size = TargetSize.Of(width, height, dpiScale);
        return device.Run(() =>
        {
            var fonts = new FreeTypeFontBackend();
            var images = new GlImageManager();
            GlSharedResources? shared = null;
            try
            {
                var pixels = Math.Max(1, (int)MathF.Round(DefaultPixels * dpiScale));
                var font = fonts.LoadFontFromMemory(EmbeddedAssets.LoadBytes(typeof(RenderedCanvasBase).Assembly, "Inter-Regular.ttf"), pixels);
                shared = new GlSharedResources(fonts, images);
                var main = GlCanvasSurface.Create(device, size, fonts, font, shared, null);
                var session = new GlCanvasSession(device, fonts, font, images, shared, main);
                device.Own(session);
                return session;
            }
            catch
            {
                shared?.Dispose();
                images.Dispose();
                fonts.Dispose();
                throw;
            }
        });
    }

    /// <inheritdoc/>
    public void Draw(Action<RenderedCanvasBase> draw, uint clearArgb = 0u) => _main.Draw(draw, clearArgb);

    /// <inheritdoc/>
    public void BeginFrame() => _main.BeginFrame();

    /// <inheritdoc/>
    public void EndFrame(uint clearArgb = 0u) => _main.EndFrame(clearArgb);

    /// <inheritdoc cref="GlOffscreenTarget.Read"/>
    public RgbaImage Read() => _main.Read();

    /// <inheritdoc/>
    public void Resize(int width, int height) => _main.Resize(width, height);

    /// <inheritdoc/>
    public void Resize(int width, int height, float dpiScale) => _main.Resize(width, height, dpiScale);

    /// <inheritdoc/>
    /// <remarks>The surface starts with the main canvas's font configuration: its default size in points, and the
    /// families and bold faces registered with it so far.</remarks>
    public ICanvasSurface CreateSurface(int width, int height, float dpiScale)
    {
        var size = TargetSize.Of(width, height, dpiScale);
        return _device.Run(() =>
        {
            ObjectDisposedException.ThrowIf(_released, this);
            var surface = GlCanvasSurface.Create(_device, size, _fonts, _font, _shared, released => _surfaces.Remove(released));
            _surfaces.Add(surface);
            surface.Canvas.CopyFontsFrom(_main.Canvas);
            return surface;
        });
    }

    /// <summary>Releases everything on the render thread. Once the device has stopped accepting work, its
    /// shutdown releases the session instead.</summary>
    /// <exception cref="InvalidOperationException">Called from inside this session's own <see cref="Draw"/>.</exception>
    public void Dispose() => _device.TryRun(() =>
    {
        if (_main.IsDrawing)
            throw new InvalidOperationException("A session cannot be disposed while it draws a frame.");
        Release();
    });

    private void Release()
    {
        if (_released) return;
        _released = true;
        _device.Disown(this);
        foreach (var surface in _surfaces.ToArray())
            surface.Release();
        _main.Release();
        _shared.Dispose();
        _images.Dispose();
        _fonts.Dispose();
    }
}
