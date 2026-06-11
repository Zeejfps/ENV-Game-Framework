using ZGF.Desktop;

namespace ZGF.Gui.Desktop;

/// <summary>
/// The per-window GUI kernel shared by every window kind (main, popup, secondary): owns the
/// canvas, input driver, build context and mounted root, and wires redraw, draw, resize and
/// DPI sync consistently — so window owners can't get the lifecycle subtly wrong.
/// </summary>
internal sealed class GuiWindowHost
{
    private readonly bool _sizeRootToWindow;

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
        bool sizeRootToWindow)
    {
        Window = window;
        Canvas = canvas;
        Input = input;
        Context = context;
        _sizeRootToWindow = sizeRootToWindow;

        input.OnAnyInput = () => window.RequestRedraw();
    }

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
                root.Width = Window.Width;
                root.Height = Window.Height;
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

    public void HandleResize(int width, int height)
    {
        Canvas.Resize(width, height);
        if (_sizeRootToWindow && Root != null)
        {
            Root.Width = width;
            Root.Height = height;
        }
    }

    public void RefreshDpiScale() => Canvas.UpdateDpiScale(Window.DpiScale);

    public void DisposeCanvas()
    {
        if (Canvas is IDisposable d) d.Dispose();
    }
}
