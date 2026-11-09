using ZGF.AppUtils;
using ZGF.Fonts;
using ZGF.Gui.OpenGL;

namespace ZGF.Gui.Testing.Gpu;

/// <summary>The shipping OpenGL canvas drawing into an offscreen target, with its own fonts, images and
/// shaders. Owned by one test; everything it holds is created and released on the device's render thread,
/// by the device's shutdown if the session is still open then.</summary>
public sealed class GlCanvasSession : IDisposable
{
    private const int DefaultPixels = 16;

    private readonly GpuDevice _device;
    private readonly FreeTypeFontBackend _fonts;
    private readonly GlImageManager _images;
    private readonly GlSharedResources _shared;
    private readonly OpenGlRenderedCanvas _canvas;
    private readonly GlOffscreenTarget _target;
    private bool _drawing;
    private bool _released;

    private GlCanvasSession(GpuDevice device, FreeTypeFontBackend fonts, GlImageManager images,
        GlSharedResources shared, OpenGlRenderedCanvas canvas, GlOffscreenTarget target)
    {
        _device = device;
        _fonts = fonts;
        _images = images;
        _shared = shared;
        _canvas = canvas;
        _target = target;
    }

    public TargetSize Size => _target.Size;

    /// <summary>The canvas, with Inter as its default font. Use it only on the render thread: inside
    /// <see cref="Draw"/> or <see cref="GpuDevice.Run(Action)"/>.</summary>
    public RenderedCanvasBase Canvas => _canvas;

    public FreeTypeFontBackend Fonts => _fonts;

    /// <exception cref="ArgumentOutOfRangeException">The size is invalid or exceeds what the context can render to.</exception>
    public static GlCanvasSession Create(GpuDevice device, int width, int height, float dpiScale = 1f)
    {
        var size = TargetSize.Of(width, height, dpiScale);
        return device.Run(() =>
        {
            var target = GlOffscreenTarget.Create(device, size);
            var fonts = new FreeTypeFontBackend();
            var images = new GlImageManager();
            GlSharedResources? shared = null;
            OpenGlRenderedCanvas? canvas = null;
            try
            {
                var pixels = Math.Max(1, (int)MathF.Round(DefaultPixels * dpiScale));
                var font = fonts.LoadFontFromMemory(EmbeddedAssets.LoadBytes(typeof(RenderedCanvasBase).Assembly, "Inter-Regular.ttf"), pixels);
                shared = new GlSharedResources(fonts, images);
                canvas = new OpenGlRenderedCanvas(width, height, fonts, font, shared, dpiScale);
                var session = new GlCanvasSession(device, fonts, images, shared, canvas, target);
                device.Own(session);
                return session;
            }
            catch
            {
                canvas?.Dispose();
                shared?.Dispose();
                images.Dispose();
                fonts.Dispose();
                target.Dispose();
                throw;
            }
        });
    }

    /// <summary>Clears the target to <paramref name="clearArgb"/> (straight alpha; transparent black by
    /// default), then draws one frame of <paramref name="draw"/> on the render thread.</summary>
    /// <exception cref="InvalidOperationException">Called from inside this session's own <see cref="Draw"/>.</exception>
    public void Draw(Action<RenderedCanvasBase> draw, uint clearArgb = 0u) => _device.Run(() =>
    {
        ObjectDisposedException.ThrowIf(_released, this);
        if (_drawing)
            throw new InvalidOperationException("This session is already drawing a frame.");
        _drawing = true;
        try
        {
            _target.Clear(clearArgb);
            _canvas.BeginFrame();
            draw(_canvas);
            _target.Bind();
            _canvas.EndFrame();
        }
        finally
        {
            _drawing = false;
        }
    });

    /// <inheritdoc cref="GlOffscreenTarget.Read"/>
    public RgbaImage Read() => _target.Read();

    /// <summary>Changes the logical size at the session's DPI scale.</summary>
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
