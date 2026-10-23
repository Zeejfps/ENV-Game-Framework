using ZGF.Desktop;
using ZGF.Gui.Testing;

namespace ZGF.Gui.Immediate.Testing;

/// <summary>Answers every picker with <see cref="Answer"/>, or cancels when it is null.</summary>
public sealed class TestFilePicker : IFilePicker
{
    public string? Answer { get; set; }
    public List<string> Titles { get; } = new();

    /// <summary>Runs as each picker opens, as the platform's modal dialog would take over.</summary>
    public Action? OnOpen { get; set; }

    public void PickFolder(string title, string? initialDirectory, Action<string> onPicked) => Pick(title, onPicked);

    public void PickFile(string title, string? initialDirectory, IReadOnlyList<FileFilter>? filters, Action<string> onPicked) => Pick(title, onPicked);

    public void PickSaveFile(string title, string? initialDirectory, string? suggestedFileName, IReadOnlyList<FileFilter>? filters, Action<string> onPicked) =>
        Pick(title, onPicked);

    private void Pick(string title, Action<string> onPicked)
    {
        Titles.Add(title);
        OnOpen?.Invoke();
        if (Answer != null)
            onPicked(Answer);
    }
}

/// <summary>Gives every popup a recording canvas of its own, as windows would, inside a screen
/// <see cref="Area"/> in UI space.</summary>
public sealed class TestPopupHost : IPopupHost
{
    private readonly Dictionary<int, RecordingCanvas> _canvases = new();

    public UiRect Area { get; } = new(-200f, -200f, 800f, 700f);
    public Dictionary<int, UiRect> Shown { get; } = new();

    /// <summary>Each shown popup's window: its rect plus the shadow around it.</summary>
    public Dictionary<int, UiRect> Surfaces { get; } = new();

    /// <summary>How many times every popup was hidden at once, as before a native dialog.</summary>
    public int HideAllCount { get; private set; }

    public RecordingCanvas CanvasOf(int id) => _canvases[id];

    public void BeginFrame()
    {
        Shown.Clear();
        Surfaces.Clear();
        Windows.Clear();
        foreach (var canvas in _canvases.Values)
            canvas.Reset();
    }

    public UiRect Place(UiRect preferred, UiRect fallback) => PopupPlacement.Fit(preferred, fallback, Area);

    public ICanvas Show(int id, UiRect rect, UiRect surface, bool passThrough)
    {
        Shown[id] = rect;
        Surfaces[id] = surface;
        if (!_canvases.TryGetValue(id, out var canvas))
            _canvases[id] = canvas = new RecordingCanvas(new SyntheticTextMeasurer());
        return canvas;
    }

    public void HideAll()
    {
        HideAllCount++;
        Shown.Clear();
        Surfaces.Clear();
    }

    private readonly Dictionary<int, UiRect> _windowPlacements = new();

    /// <summary>Each window shown this frame: where it is, centered in <see cref="Area"/> until placed.
    /// Its UI space is the screen's, so bounds it opens at are rects in it.</summary>
    public Dictionary<int, UiRect> Windows { get; } = new();

    /// <summary>The windows whose close button was pressed, answered on their next frame.</summary>
    public HashSet<int> CloseRequests { get; } = new();

    WindowSurface? IPopupHost.ShowWindow(int id, string title, float width, float height, WindowBounds? openAt, UiRect? placeAt)
    {
        if (openAt != null && !_windowPlacements.ContainsKey(id))
            _windowPlacements[id] = new UiRect(openAt.Value.X, openAt.Value.Y, openAt.Value.Width, openAt.Value.Height);
        if (placeAt != null)
            _windowPlacements[id] = placeAt.Value;
        var rect = new UiRect(Area.X + (Area.Width - width) / 2, Area.Y + (Area.Height - height) / 2, width, height);
        if (_windowPlacements.TryGetValue(id, out var placed))
            rect = placed;
        Windows[id] = rect;
        if (!_canvases.TryGetValue(id, out var canvas))
            _canvases[id] = canvas = new RecordingCanvas(new SyntheticTextMeasurer());
        var closeRequested = CloseRequests.Remove(id);
        var bounds = new WindowBounds((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, false);
        return new WindowSurface(canvas, rect, bounds, closeRequested);
    }
}
