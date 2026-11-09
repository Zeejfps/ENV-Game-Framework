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
    private readonly MetalImageManager _images;
    private readonly MetalSharedResources _shared;
    private readonly MetalRenderedCanvas _canvas;
    private readonly MetalOffscreenTarget _target;
    private bool _drawing;
    private bool _released;

    private MetalCanvasSession(GpuDevice device, FreeTypeFontBackend fonts, MetalImageManager images,
        MetalSharedResources shared, MetalRenderedCanvas canvas, MetalOffscreenTarget target)
    {
        _device = device;
        _fonts = fonts;
        _images = images;
        _shared = shared;
        _canvas = canvas;
        _target = target;
    }

    public TargetSize Size => _target.Size;

    /// <inheritdoc/>
    public RenderedCanvasBase Canvas => _canvas;

    public FreeTypeFontBackend Fonts => _fonts;

    /// <exception cref="ArgumentOutOfRangeException">The size is invalid or exceeds what the GPU can render to.</exception>
    /// <exception cref="InvalidOperationException">The device is not a Metal device.</exception>
    public static MetalCanvasSession Create(GpuDevice device, int width, int height, float dpiScale = 1f)
    {
        var metal = device.Metal;
        var size = TargetSize.Of(width, height, dpiScale);
        return device.Run(() =>
        {
            var target = MetalOffscreenTarget.Create(device, size);
            var fonts = new FreeTypeFontBackend();
            MetalImageManager? images = null;
            MetalSharedResources? shared = null;
            MetalRenderedCanvas? canvas = null;
            try
            {
                var pixels = Math.Max(1, (int)MathF.Round(DefaultPixels * dpiScale));
                var font = fonts.LoadFontFromMemory(EmbeddedAssets.LoadBytes(typeof(RenderedCanvasBase).Assembly, "Inter-Regular.ttf"), pixels);
                images = new MetalImageManager(metal.Device, metal.CommandQueue);
                shared = new MetalSharedResources(metal.Device, metal.CommandQueue, fonts, images);
                canvas = new MetalRenderedCanvas(width, height, fonts, font, shared, dpiScale);
                var session = new MetalCanvasSession(device, fonts, images, shared, canvas, target);
                device.Own(session);
                return session;
            }
            catch
            {
                canvas?.Dispose();
                shared?.Dispose();
                images?.Dispose();
                fonts.Dispose();
                target.Dispose();
                throw;
            }
        });
    }

    /// <inheritdoc/>
    public void Draw(Action<RenderedCanvasBase> draw, uint clearArgb = 0u) => _device.Run(() =>
    {
        ObjectDisposedException.ThrowIf(_released, this);
        if (_drawing)
            throw new InvalidOperationException("This session is already drawing a frame.");
        _drawing = true;
        try
        {
            _canvas.BeginFrame();
            draw(_canvas);
            _target.Render(clearArgb, _canvas.EndFrame);
        }
        finally
        {
            _drawing = false;
        }
    });

    /// <inheritdoc/>
    public RgbaImage Read() => _target.Read();

    /// <inheritdoc/>
    public void Resize(int width, int height)
    {
        var size = TargetSize.Of(width, height, Size.DpiScale);
        _device.Run(() =>
        {
            ObjectDisposedException.ThrowIf(_released, this);
            if (_drawing)
                throw new InvalidOperationException("A session cannot be resized while it draws a frame.");
            _target.Resize(size);
            _canvas.Resize(width, height);
        });
    }

    /// <summary>Releases everything on the render thread. Once the device has stopped accepting work, its
    /// shutdown releases the session instead.</summary>
    /// <exception cref="InvalidOperationException">Called from inside this session's own <see cref="Draw"/>.</exception>
    public void Dispose() => _device.TryRun(() =>
    {
        if (_drawing)
            throw new InvalidOperationException("A session cannot be disposed while it draws a frame.");
        Release();
    });

    private void Release()
    {
        if (_released) return;
        _released = true;
        _device.Disown(this);
        _canvas.Dispose();
        _shared.Dispose();
        _images.Dispose();
        _fonts.Dispose();
        _target.Dispose();
    }
}
