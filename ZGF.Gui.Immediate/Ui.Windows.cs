using ZGF.Desktop;

namespace ZGF.Gui.Immediate;

public abstract partial class Ui
{
    private const int WindowZOffset = ModalZStep / 2;

    private int _surfaces;
    private readonly Dictionary<int, UiRect> _windowPlacements = new();

    /// <summary>
    /// Opens a window of its own for what is recorded inside, titled <paramref name="title"/>:
    /// <paramref name="width"/> by <paramref name="height"/> points, centered on the main window when
    /// it opens, unless <paramref name="bounds"/> says where it was left, on the screen; each frame
    /// <paramref name="bounds"/> becomes where it is, for the app to keep. It has no frame of the platform's, so what is recorded draws its title and close
    /// button, and moves and sizes it with <see cref="PlaceWindow"/>. The platform keeps it in front of the main
    /// window, which takes no input while it is shown, so record it inside a <see cref="Modal"/>.
    /// Coordinates stay the main window's UI space, and the element is hit-testable as
    /// <paramref name="id"/>. Where the platform has no windows to give, <paramref name="native"/> is
    /// false and it is drawn centered over the main window instead, no bigger than it. A request of the
    /// platform's to close it, such as Alt+F4, sets <paramref name="open"/> to false.
    /// </summary>
    public Scope Window(int id, string title, ref bool open, float width, float height, ref WindowBounds? bounds, out bool native)
    {
        UiRect? placeAt = null;
        if (_windowPlacements.Remove(id, out var placement))
            placeAt = placement;
        var surface = _popupHost?.ShowWindow(id, title, width, height, bounds, placeAt);
        native = surface != null;
        if (surface != null)
            bounds = surface.Value.Bounds;
        var rect = surface != null ? surface.Value.Rect : CenterOverWindow(width, height);
        if (surface?.CloseRequested == true)
            CloseWindow(ref open);

        var e = Describe(id, new Box { Width = Sizing.Fixed(rect.Width), Height = Sizing.Fixed(rect.Height) }, null, null);
        e.Parent = 0;
        e.Floating = true;
        e.OffsetX = rect.X;
        e.OffsetY = rect.Y;
        e.Z = ModalZ + _modalCount * ModalZStep - WindowZOffset;
        if (surface != null)
        {
            e.Layer = ++_surfaces;
            _tree.SetLayer(e.Layer, surface.Value.Canvas, rect);
        }
        Push(e);
        return BeginScope(ScopeKind.Element);
    }

    /// <summary>Moves and sizes the <see cref="Window"/> with <paramref name="id"/> to
    /// <paramref name="rect"/>, in the main window's UI space, from the next frame.</summary>
    public void PlaceWindow(int id, UiRect rect)
    {
        _windowPlacements[id] = rect;
        RequestUpdate();
    }

    private UiRect CenterOverWindow(float width, float height)
    {
        var w = MathF.Min(width, Width);
        var h = MathF.Min(height, Height);
        return new UiRect(MathF.Floor((Width - w) / 2), MathF.Floor((Height - h) / 2), w, h);
    }

    private void CloseWindow(ref bool open)
    {
        open = false;
        RequestUpdate();
    }
}
