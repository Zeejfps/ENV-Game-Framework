using System.Runtime.Versioning;
using ZGF.AppUtils;
using ZGF.Fonts;
using ZGF.Gui.Metal;

namespace ZGF.Gui.Testing.Gpu.Apple;

/// <summary>The shipping Metal canvas drawing into a private-storage texture.</summary>
[SupportedOSPlatform("macos")]
public sealed class MetalCanvasSession : ICanvasSession
{
    private const int DefaultPixels = 16;

    private readonly GpuDevice _device;
    private readonly FreeTypeFontBackend _fonts;
    private readonly FontHandle _font;
    private readonly MetalImageManager _images;
    private readonly MetalSharedResources _shared;
    private readonly MetalCanvasSurface _main;
    private readonly List<MetalCanvasSurface> _surfaces = [];
    private bool _released;

    private MetalCanvasSession(GpuDevice device, FreeTypeFontBackend fonts, FontHandle font, MetalImageManager images,
        MetalSharedResources shared, MetalCanvasSurface main)
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

    /// <inheritdoc/>
    public int RetiredImageCount => _device.Run(() => _images.RetiredCount);

    /// <exception cref="ArgumentOutOfRangeException">The size is invalid or exceeds what the GPU can render to.</exception>
    /// <exception cref="InvalidOperationException">The device is not a Metal device.</exception>
    public static MetalCanvasSession Create(GpuDevice device, int width, int height, float dpiScale = 1f)
    {
        var metal = device.Metal;
        var size = TargetSize.Of(width, height, dpiScale);
        return device.Run(() =>
        {
            var fonts = new FreeTypeFontBackend();
            MetalImageManager? images = null;
            MetalSharedResources? shared = null;
            try
            {
                var pixels = Math.Max(1, (int)MathF.Round(DefaultPixels * dpiScale));
                var font = fonts.LoadFontFromMemory(EmbeddedAssets.LoadBytes(typeof(RenderedCanvasBase).Assembly, "Inter-Regular.ttf"), pixels);
                images = new MetalImageManager(metal.Device, metal.CommandQueue);
                shared = new MetalSharedResources(metal.Device, metal.CommandQueue, fonts, images);
                var main = MetalCanvasSurface.Create(device, size, fonts, font, shared, null);
                var session = new MetalCanvasSession(device, fonts, font, images, shared, main);
                device.Own(session);
                return session;
            }
            catch
            {
                shared?.Dispose();
                images?.Dispose();
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

    /// <inheritdoc/>
    public void Replay(uint clearArgb = 0u) => _main.Replay(clearArgb);

    /// <inheritdoc/>
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
            var surface = MetalCanvasSurface.Create(_device, size, _fonts, _font, _shared, released => _surfaces.Remove(released));
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
