namespace ZGF.Gui.Testing.Gpu;

/// <summary>A shipping canvas drawing into an offscreen target of its own. Its members marshal to the device's
/// render thread.</summary>
public interface ICanvasSurface : IDisposable
{
    TargetSize Size { get; }

    /// <summary>Use it only on the render thread, between <see cref="BeginFrame"/> and <see cref="EndFrame"/>,
    /// inside a session's <see cref="ICanvasSession.Draw"/> or in <see cref="GpuDevice.Run(Action)"/>.</summary>
    RenderedCanvasBase Canvas { get; }

    /// <summary>Starts a frame on <see cref="Canvas"/>, discarding one begun and never ended.</summary>
    /// <exception cref="InvalidOperationException">Called from inside a session's own <see cref="ICanvasSession.Draw"/>.</exception>
    void BeginFrame();

    /// <summary>Clears the target to <paramref name="clearArgb"/> (straight alpha; transparent black by default)
    /// and draws what the frame begun with <see cref="BeginFrame"/> recorded.</summary>
    /// <exception cref="InvalidOperationException">No frame was begun.</exception>
    void EndFrame(uint clearArgb = 0u);

    /// <summary>Clears the target and draws the last frame again without beginning a new one, as a window's
    /// present-only redraw does.</summary>
    /// <exception cref="InvalidOperationException">A frame begun with <see cref="BeginFrame"/> has not ended.</exception>
    void Replay(uint clearArgb = 0u);

    /// <summary>The target's pixels: RGBA, rows top-down, premultiplied alpha.</summary>
    RgbaImage Read();

    /// <summary>Changes the logical size at the current DPI scale, discarding a frame begun and not ended.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The size is invalid or exceeds what the device can render to.</exception>
    void Resize(int width, int height);

    /// <summary>Changes the logical size and the DPI scale, discarding a frame begun and not ended.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The size or scale is invalid or exceeds what the device can render to.</exception>
    void Resize(int width, int height, float dpiScale);
}
