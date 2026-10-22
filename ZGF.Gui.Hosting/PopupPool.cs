using ZGF.Desktop;

namespace ZGF.Gui.Hosting;

/// <summary>A borderless popup window and the canvas drawn on it.</summary>
internal sealed record PopupSurface(IWindow Window, RenderedCanvasBase Canvas);

/// <summary>Hidden popup windows kept for reuse, since creating a window and its graphics context is
/// slow. A window that served a tooltip can serve a menu next, so each acquire sets it up afresh.</summary>
internal sealed class PopupPool : IDisposable
{
    private const int InitialSize = 32;

    private readonly IGuiRenderBackend _backend;
    private readonly int _capacity;
    private readonly List<PopupSurface> _idle = new();

    public PopupPool(IGuiRenderBackend backend, int capacity)
    {
        _backend = backend;
        _capacity = capacity;
    }

    /// <summary>A hidden popup that draws with the main canvas's current fonts and lets the pointer
    /// through only when <paramref name="mousePassThrough"/>.</summary>
    public PopupSurface Acquire(bool mousePassThrough)
    {
        var surface = TakeIdle() ?? Create(mousePassThrough);
        // Re-applied on every acquire: a window left click-through by a tooltip would otherwise give
        // the menu reusing it hover but no clicks.
        surface.Window.SetMousePassThrough(mousePassThrough);
        surface.Canvas.CopyFontsFrom(_backend.MainCanvas);
        return surface;
    }

    private PopupSurface? TakeIdle()
    {
        if (_idle.Count == 0) return null;
        var last = _idle.Count - 1;
        var surface = _idle[last];
        _idle.RemoveAt(last);
        return surface;
    }

    private PopupSurface Create(bool mousePassThrough)
    {
        var app = _backend.App;
        var window = app.CreatePopupWindow(new PopupWindowOptions
        {
            WidthPoints = InitialSize,
            HeightPoints = InitialSize,
            OwnerWindow = app.MainWindow,
            MousePassThrough = mousePassThrough,
        });
        var canvas = _backend.CreateCanvas(window, InitialSize, InitialSize);
        return new PopupSurface(window, canvas);
    }

    /// <summary>Hides <paramref name="surface"/> and keeps it for reuse, or destroys it once the pool
    /// is full.</summary>
    public void Release(PopupSurface surface)
    {
        surface.Window.Hide();
        if (_idle.Count < _capacity)
        {
            _idle.Add(surface);
            return;
        }
        _backend.DisposeWindow(surface.Window, surface.Canvas);
    }

    public void Dispose()
    {
        foreach (var surface in _idle)
            _backend.DisposeWindow(surface.Window, surface.Canvas);
        _idle.Clear();
    }
}
