using System.Runtime.Versioning;
using ZGF.Fonts;
using ZGF.Gui.Metal;

namespace ZGF.Gui.Testing.Gpu.Apple;

/// <summary>A Metal canvas drawing into a private-storage texture of its own, with fonts, images and shaders it
/// is given.</summary>
[SupportedOSPlatform("macos")]
internal sealed class MetalCanvasSurface : ICanvasSurface
{
    private readonly GpuDevice _device;
    private readonly MetalRenderedCanvas _canvas;
    private readonly MetalOffscreenTarget _target;
    private readonly Action<MetalCanvasSurface>? _onReleased;
    private bool _drawing;
    private bool _frameOpen;
    private bool _released;

    private MetalCanvasSurface(GpuDevice device, MetalRenderedCanvas canvas, MetalOffscreenTarget target, Action<MetalCanvasSurface>? onReleased)
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
    public static MetalCanvasSurface Create(GpuDevice device, TargetSize size, FreeTypeFontBackend fonts, FontHandle font,
        MetalSharedResources shared, Action<MetalCanvasSurface>? onReleased)
    {
        var target = MetalOffscreenTarget.Create(device, size);
        try
        {
            var canvas = new MetalRenderedCanvas(size.Width, size.Height, fonts, font, shared, size.DpiScale);
            return new MetalCanvasSurface(device, canvas, target, onReleased);
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
            _target.Render(clearArgb, _canvas.EndFrame);
        }
        catch
        {
            _canvas.AbortFrame();
            throw;
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
        _target.Render(clearArgb, _canvas.EndFrame);
    });

    public void Replay(uint clearArgb = 0u) => _device.Run(() =>
    {
        ObjectDisposedException.ThrowIf(_released, this);
        if (_drawing || _frameOpen)
            throw new InvalidOperationException("A frame begun with BeginFrame has not ended.");
        _target.Render(clearArgb, _canvas.EndFrame);
    });

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
            if (_frameOpen)
                _canvas.AbortFrame();
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
