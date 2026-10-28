using ZGF.Desktop;
using ZGF.Fonts;
using ZGF.Gui.Hosting;
using ZGF.Gui.Testing;

namespace ZGF.Gui.Immediate.Testing;

/// <summary>Answers every picker with <see cref="Answer"/>, or cancels when it is null.</summary>
public sealed class TestFilePicker : IFilePicker
{
    public string? Answer { get; set; }
    public List<string> Titles { get; } = new();

    /// <summary>The file name and filters the last save picker offered.</summary>
    public (string? FileName, IReadOnlyList<FileFilter>? Filters) LastSave { get; private set; }

    /// <summary>Runs as each picker opens, as the platform's modal dialog would take over.</summary>
    public Action? OnOpen { get; set; }

    public void PickFolder(string title, string? initialDirectory, Action<string> onPicked) => Pick(title, onPicked);

    public void PickFile(string title, string? initialDirectory, IReadOnlyList<FileFilter>? filters, Action<string> onPicked) => Pick(title, onPicked);

    public void PickSaveFile(string title, string? initialDirectory, string? suggestedFileName, IReadOnlyList<FileFilter>? filters, Action<string> onPicked)
    {
        LastSave = (suggestedFileName, filters);
        Pick(title, onPicked);
    }

    private void Pick(string title, Action<string> onPicked)
    {
        Titles.Add(title);
        OnOpen?.Invoke();
        if (Answer != null)
            onPicked(Answer);
    }
}

/// <summary>Lists <see cref="Families"/> as the installed fonts.</summary>
public sealed class TestSystemFonts : ISystemFonts
{
    public List<SystemFontFamily> Families { get; } = new();

    public IReadOnlyList<SystemFontFamily> ListFamilies() => Families.ToArray();
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

/// <summary>Stands in for macOS's menu bar: keeps the menus last shown, and picks their items as the
/// user would.</summary>
public sealed class TestSystemMenuBar : ISystemMenuBar
{
    private Action<AppMenuItem>? _picked;

    /// <summary>The menus last shown; as on macOS, their items' enabled and checked states change in
    /// place between showings.</summary>
    public IReadOnlyList<AppMenu> Menus { get; private set; } = [];

    /// <summary>How many times the menus were shown.</summary>
    public int Showings { get; private set; }

    public void Show(IReadOnlyList<AppMenu> menus, Action<AppMenuItem> picked)
    {
        Menus = menus;
        _picked = picked;
        Showings++;
    }

    /// <summary>The menu reached through the menus and submenus titled <paramref name="titles"/>.</summary>
    public AppMenu Menu(params string[] titles)
    {
        var menu = Menus.Single(m => m.Title == titles[0]);
        foreach (var title in titles.AsSpan(1))
            menu = menu.Entries.OfType<AppMenu>().Single(m => m.Title == title);
        return menu;
    }

    /// <summary>The item titled last in <paramref name="path"/>, in the menu the titles before it reach.</summary>
    public AppMenuItem Item(params string[] path) =>
        Menu(path[..^1]).Entries.OfType<AppMenuItem>().Single(i => i.Title == path[^1]);

    /// <summary>Picks the item <paramref name="path"/> names, as clicking it in the menu bar would.</summary>
    public void Pick(params string[] path) => _picked!(Item(path));
}
