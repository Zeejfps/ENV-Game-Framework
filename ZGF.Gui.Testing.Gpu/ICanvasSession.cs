using ZGF.Fonts;

namespace ZGF.Gui.Testing.Gpu;

/// <summary>A shipping canvas drawing into an offscreen target on a <see cref="GpuDevice"/>, with its own
/// fonts, images and shaders, which the surfaces it creates share. Made by <see cref="GpuDevice.CreateSession"/>;
/// everything it holds is created and released on the device's render thread, by the device's shutdown if the
/// session is still open then.</summary>
public interface ICanvasSession : ICanvasSurface
{
    /// <summary>The fonts of <see cref="ICanvasSurface.Canvas"/>, whose default font is Inter, and of every surface
    /// the session creates.</summary>
    FreeTypeFontBackend Fonts { get; }

    /// <summary>Clears the target to <paramref name="clearArgb"/> (straight alpha; transparent black by
    /// default), then draws one frame of <paramref name="draw"/> on the render thread.</summary>
    /// <exception cref="InvalidOperationException">Called from inside this session's own <see cref="Draw"/>.</exception>
    void Draw(Action<RenderedCanvasBase> draw, uint clearArgb = 0u);

    /// <summary>Another canvas, drawing into a target of its own, that shares this session's fonts, images and
    /// shaders, as a popup window's canvas shares its main window's, and starts with <see cref="ICanvasSurface.Canvas"/>'s
    /// font configuration: its default size in points and the families and bold faces registered so far. Disposing
    /// the session disposes it.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The size or scale is invalid or exceeds what the device can render to.</exception>
    ICanvasSurface CreateSurface(int width, int height, float dpiScale);
}
