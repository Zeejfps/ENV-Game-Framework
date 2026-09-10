using ZGF.Desktop;
using ZGF.Geometry;

namespace ZGF.Gui.Desktop;

/// <summary>
/// The per-window GUI kernel shared by every window kind (main, popup, secondary): owns the
/// canvas, input driver, build context and mounted root, and wires redraw, draw, resize and
/// scale sync consistently — so window owners can't get the lifecycle subtly wrong.
/// </summary>
internal sealed class GuiWindowHost
{
    private readonly bool _sizeRootToWindow;
    private readonly IUiScale _uiScale;

    public IWindow Window { get; }
    public RenderedCanvasBase Canvas { get; }
    public DesktopInputSystem Input { get; }
    public Context Context { get; }
    public View? Root { get; private set; }

    /// <param name="sizeRootToWindow">True for windows whose root fills the client area
    /// (main, secondary); false for popups, whose content sizes the window instead.</param>
    public GuiWindowHost(
        IWindow window,
        RenderedCanvasBase canvas,
        DesktopInputSystem input,
        Context context,
        IUiScale uiScale,
        bool sizeRootToWindow)
    {
        Window = window;
        Canvas = canvas;
        Input = input;
        Context = context;
        _uiScale = uiScale;
        _sizeRootToWindow = sizeRootToWindow;

        input.OnAnyInput = () => window.RequestRedraw();

        // The canvas is created at the size the window was asked for in screen coordinates, which is
        // the logical size only at scale 1. Bring the two into agreement before anything measures.
        SyncScale();
    }

    /// <summary>This window's current conversion between the canvas, the window, the desktop and the
    /// framebuffer. A snapshot — take a fresh one rather than holding this across anything that can
    /// move or resize the window.</summary>
    public WindowSpace Space => WindowSpace.Of(Window, Window.ContentScale * _uiScale.Value);

    public void SetRoot(View? root)
    {
        if (Root != null)
        {
            Root.Unmount();
            Root.OnRedrawNeeded = null;
        }
        Root = root;
        if (root != null)
        {
            if (_sizeRootToWindow)
            {
                root.Width = Canvas.Width;
                root.Height = Canvas.Height;
            }
            root.OnRedrawNeeded = Window.RequestRedraw;
            root.Mount();
        }
    }

    public void DrawContent()
    {
        if (Root == null) return;
        Root.LayoutSelf();
        Root.DrawSelf(Canvas);
    }

    /// <summary>Re-derives the canvas's scale and size, and the root's, from the window's current
    /// framebuffer. Call after a resize, on a UI-scale change, and after moving to a monitor of a
    /// different content scale: layout is in logical points, so a scale the canvas has not been
    /// re-sized for changes nothing on screen.</summary>
    public void SyncScale() => Apply(Space);

    /// <summary>Sizes the canvas for a window that has just been told to become
    /// <paramref name="screenSize"/> — the popup case, where the framebuffer may not have caught up
    /// with the size the OS was handed.</summary>
    public void ResizeTo(ScreenSize screenSize) => Apply(Space.WithWindowSize(screenSize));

    public void DisposeCanvas()
    {
        if (Canvas is IDisposable d) d.Dispose();
    }

    private void Apply(WindowSpace space)
    {
        Canvas.UpdateDpiScale(space.Scale);
        Canvas.Resize((int)space.CanvasSize.Width, (int)space.CanvasSize.Height);

        if (!_sizeRootToWindow || Root == null) return;
        Root.Width = Canvas.Width;
        Root.Height = Canvas.Height;
    }
}
