using ZGF.Fonts;

namespace ZGF.Gui.Testing.Gpu;

/// <summary>A shipping canvas drawing into an offscreen target on a <see cref="GpuDevice"/>, with its own
/// fonts, images and shaders. Made by <see cref="GpuDevice.CreateSession"/>; everything it holds is created
/// and released on the device's render thread, by the device's shutdown if the session is still open then.</summary>
public interface ICanvasSession : IDisposable
{
    TargetSize Size { get; }

    /// <summary>The canvas, with Inter as its default font. Use it only on the render thread: inside
    /// <see cref="Draw"/> or <see cref="GpuDevice.Run(Action)"/>.</summary>
    RenderedCanvasBase Canvas { get; }

    FreeTypeFontBackend Fonts { get; }

    /// <summary>Clears the target to <paramref name="clearArgb"/> (straight alpha; transparent black by
    /// default), then draws one frame of <paramref name="draw"/> on the render thread.</summary>
    /// <exception cref="InvalidOperationException">Called from inside this session's own <see cref="Draw"/>.</exception>
    void Draw(Action<RenderedCanvasBase> draw, uint clearArgb = 0u);

    /// <summary>The target's pixels: RGBA, rows top-down, premultiplied alpha.</summary>
    RgbaImage Read();

    /// <summary>Changes the logical size at the session's DPI scale.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The size is invalid or exceeds what the device can render to.</exception>
    void Resize(int width, int height);
}
