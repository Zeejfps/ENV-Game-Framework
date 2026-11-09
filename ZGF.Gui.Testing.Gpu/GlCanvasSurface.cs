using ZGF.Fonts;
using ZGF.Gui.OpenGL;

namespace ZGF.Gui.Testing.Gpu;

/// <summary>An OpenGL canvas drawing into an FBO of its own, with fonts, images and shaders it is given.</summary>
internal sealed class GlCanvasSurface : ICanvasSurface
{
    private readonly GpuDevice _device;
    private readonly OpenGlRenderedCanvas _canvas;
    private readonly GlOffscreenTarget _target;
    private readonly Action<GlCanvasSurface>? _onReleased;
    private bool _drawing;
    private bool _frameOpen;
    private bool _released;

    private GlCanvasSurface(GpuDevice device, OpenGlRenderedCanvas canvas, GlOffscreenTarget target, Action<GlCanvasSurface>? onReleased)
    {
        _device = device;
        _canvas = canvas;
        _target = target;
        _onReleased = onReleased;
    }

    public TargetSize Size => _target.Size;

    public RenderedCanvasBase Canvas => _canvas;

    /// <summary>Whether <see cref="Draw"/> is running its callback.</summary>
    public bool IsDrawing => _drawing;

    /// <summary>Render thread only.</summary>
    public static GlCanvasSurface Create(GpuDevice device, TargetSize size, FreeTypeFontBackend fonts, FontHandle font,
        GlSharedResources shared, Action<GlCanvasSurface>? onReleased)
    {
        var target = GlOffscreenTarget.Create(device, size);
        try
        {
            var canvas = new OpenGlRenderedCanvas(size.Width, size.Height, fonts, font, shared, size.DpiScale);
            return new GlCanvasSurface(device, canvas, target, onReleased);
        }
        catch
        {
            target.Dispose();
            throw;
        }
    }

    public void Draw(Action<RenderedCanvasBase> draw, uint clearArgb) => _device.Run(() =>
    {
        ObjectDisposedException.ThrowIf(_released, this);
        if (_drawing)
            throw new InvalidOperationException("This session is already drawing a frame.");
        _drawing = true;
        try
        {
            _canvas.BeginFrame();
            draw(_canvas);
            Flush(clearArgb);
        }
        finally
        {
            _drawing = false;
            _frameOpen = false;
        }
    });

    public void BeginFrame() => _device.Run(() =>
    {
        ObjectDisposedException.ThrowIf(_released, this);
        if (_drawing)
            throw new InvalidOperationException("This session is already drawing a frame.");
        _canvas.BeginFrame();
        _frameOpen = true;
    });

    public void EndFrame(uint clearArgb = 0u) => _device.Run(() =>
    {
        ObjectDisposedException.ThrowIf(_released, this);
        if (_drawing || !_frameOpen)
            throw new InvalidOperationException("No frame was begun with BeginFrame.");
        _frameOpen = false;
        Flush(clearArgb);
    });

    private void Flush(uint clearArgb)
    {
        _target.Clear(clearArgb);
        _target.Bind();
        _canvas.EndFrame();
    }

    public RgbaImage Read() => _target.Read();

    public void Resize(int width, int height) => Resize(width, height, Size.DpiScale);

    public void Resize(int width, int height, float dpiScale)
    {
        var size = TargetSize.Of(width, height, dpiScale);
        _device.Run(() =>
        {
            ObjectDisposedException.ThrowIf(_released, this);
            if (_drawing)
                throw new InvalidOperationException("A session cannot be resized while it draws a frame.");
            _frameOpen = false;
            _target.Resize(size);
            _canvas.UpdateDpiScale(dpiScale);
            _canvas.Resize(width, height);
        });
    }

    /// <exception cref="InvalidOperationException">Called from inside <see cref="Draw"/>.</exception>
    public void Dispose() => _device.TryRun(() =>
    {
        if (_drawing)
            throw new InvalidOperationException("A session cannot be disposed while it draws a frame.");
        Release();
    });

    /// <summary>Render thread only.</summary>
    public void Release()
    {
        if (_released) return;
        _released = true;
        _canvas.Dispose();
        _target.Dispose();
        _onReleased?.Invoke(this);
    }
}
