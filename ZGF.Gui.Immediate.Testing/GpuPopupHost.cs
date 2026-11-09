using ZGF.Desktop;
using ZGF.Gui.Testing.Gpu;

namespace ZGF.Gui.Immediate.Testing;

/// <summary>A popup or window shown this frame: what it is, where its canvas's top-left is in the main window's UI
/// points, and the offscreen surface it drew on.</summary>
internal sealed record ShownSurface(SurfaceKind Kind, UiRect Rect, ICanvasSurface Surface, CapturingCanvas Canvas);

/// <summary>Gives every popup and <see cref="Ui.Window"/> an offscreen surface of its own, sharing the main
/// session's fonts and images, placed inside a fake monitor <see cref="Area"/> in the main window's UI space,
/// where a screen unit is a UI point. Render thread only.</summary>
internal sealed class GpuPopupHost(ICanvasSession session) : IPopupHost
{
    private readonly Dictionary<(SurfaceKind Kind, int Id), (ICanvasSurface Surface, CapturingCanvas Canvas)> _surfaces = new();
    private readonly HashSet<(SurfaceKind Kind, int Id)> _used = new();
    private readonly List<ShownSurface> _shown = new();
    private readonly Dictionary<int, UiRect> _windowPlacements = new();

    /// <summary>The monitor popups are kept on and windows centered in.</summary>
    public UiRect Area { get; set; }

    /// <summary>Runs on the render thread after a surface no popup or window showed is disposed.</summary>
    public Action? SurfaceReleased { get; set; }

    /// <summary>Windows, then popups, in the order shown this frame: the order they are stacked in.</summary>
    public IEnumerable<ShownSurface> Shown => _shown.Where(s => s.Kind == SurfaceKind.Window).Concat(_shown.Where(s => s.Kind == SurfaceKind.Popup));

    private float DpiScale => session.Size.DpiScale;

    /// <summary>The surface shown this frame whose canvas is <paramref name="canvas"/>, or null.</summary>
    public ShownSurface? ShownOn(ICanvas canvas)
    {
        foreach (var shown in _shown)
        {
            if (shown.Canvas == canvas)
                return shown;
        }
        return null;
    }

    public void BeginFrame()
    {
        _shown.Clear();
        _used.Clear();
    }

    /// <summary>Draws every surface shown this frame, and releases those that were not, forgetting where a closed
    /// window was placed.</summary>
    public void EndFrame()
    {
        foreach (var shown in _shown)
            shown.Surface.EndFrame();
        foreach (var key in _surfaces.Keys.Where(key => !_used.Contains(key)).ToList())
        {
            _surfaces[key].Surface.Dispose();
            _surfaces.Remove(key);
            if (key.Kind == SurfaceKind.Window)
                _windowPlacements.Remove(key.Id);
            SurfaceReleased?.Invoke();
        }
    }

    /// <summary>Has every surface draw with the main canvas's fonts, after they changed.</summary>
    public void FollowFonts()
    {
        foreach (var (surface, _) in _surfaces.Values)
            surface.Canvas.CopyFontsFrom(session.Canvas);
    }

    public UiRect Place(UiRect preferred, UiRect fallback) =>
        PopupPlacement.SnapToPixels(PopupPlacement.Fit(preferred, fallback, Area), DpiScale);

    public ICanvas Show(int id, UiRect rect, UiRect surface, bool passThrough) =>
        Begin(SurfaceKind.Popup, id, surface, (int)MathF.Ceiling(surface.Width), (int)MathF.Ceiling(surface.Height));

    /// <summary>Hides the popups, as before a native dialog; windows stay.</summary>
    public void HideAll() => _shown.RemoveAll(s => s.Kind == SurfaceKind.Popup);

    WindowSurface? IPopupHost.ShowWindow(int id, string title, float width, float height, WindowBounds? openAt, UiRect? placeAt)
    {
        if (openAt != null && !_windowPlacements.ContainsKey(id))
            _windowPlacements[id] = new UiRect(openAt.Value.X, openAt.Value.Y, openAt.Value.Width, openAt.Value.Height);
        if (placeAt != null)
            _windowPlacements[id] = placeAt.Value;
        var wanted = _windowPlacements.TryGetValue(id, out var placed)
            ? placed
            : new UiRect(Area.X + (Area.Width - width) / 2, Area.Y + (Area.Height - height) / 2, width, height);
        var rect = WholePoints(wanted);
        _windowPlacements[id] = rect;
        var canvas = Begin(SurfaceKind.Window, id, rect, (int)rect.Width, (int)rect.Height);
        var bounds = new WindowBounds((int)MathF.Round(rect.X), (int)MathF.Round(rect.Y), (int)rect.Width, (int)rect.Height, false);
        return new WindowSurface(canvas, rect, bounds, false);
    }

    /// <summary><paramref name="rect"/> as a window holds it: a whole number of points, as its canvas has, at least
    /// one each way, with its top-left on the device pixel grid.</summary>
    private UiRect WholePoints(UiRect rect)
    {
        var snapped = PopupPlacement.SnapToPixels(rect, DpiScale);
        return snapped with { Width = MathF.Max(1f, MathF.Round(rect.Width)), Height = MathF.Max(1f, MathF.Round(rect.Height)) };
    }

    private CapturingCanvas Begin(SurfaceKind kind, int id, UiRect rect, int width, int height)
    {
        var key = (kind, id);
        var (surface, canvas) = Acquire(key, Math.Max(1, width), Math.Max(1, height));
        _used.Add(key);
        surface.BeginFrame();
        canvas.Reset(kind, rect);
        _shown.RemoveAll(s => s.Surface == surface);
        _shown.Add(new ShownSurface(kind, rect, surface, canvas));
        return canvas;
    }

    private (ICanvasSurface Surface, CapturingCanvas Canvas) Acquire((SurfaceKind, int) key, int width, int height)
    {
        if (_surfaces.TryGetValue(key, out var existing))
        {
            var size = existing.Surface.Size;
            if (size.Width != width || size.Height != height || size.DpiScale != DpiScale)
                existing.Surface.Resize(width, height, DpiScale);
            return existing;
        }
        var surface = session.CreateSurface(width, height, DpiScale);
        var created = (surface, new CapturingCanvas(surface.Canvas));
        _surfaces[key] = created;
        return created;
    }
}
