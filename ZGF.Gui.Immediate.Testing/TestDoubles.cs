using ZGF.Desktop;
using ZGF.Gui.Testing;

namespace ZGF.Gui.Immediate.Testing;

public sealed class TestClipboard : IClipboard
{
    public string? Text { get; set; }

    public void SetText(string text) => Text = text;

    public string? GetText() => Text;
}

/// <summary>Answers every picker with <see cref="Answer"/>, or cancels when it is null.</summary>
public sealed class TestFilePicker : IFilePicker
{
    public string? Answer { get; set; }
    public List<string> Titles { get; } = new();

    public void PickFolder(string title, string? initialDirectory, Action<string> onPicked) => Pick(title, onPicked);

    public void PickFile(string title, string? initialDirectory, IReadOnlyList<FileFilter>? filters, Action<string> onPicked) => Pick(title, onPicked);

    public void PickSaveFile(string title, string? initialDirectory, string? suggestedFileName, IReadOnlyList<FileFilter>? filters, Action<string> onPicked) =>
        Pick(title, onPicked);

    private void Pick(string title, Action<string> onPicked)
    {
        Titles.Add(title);
        if (Answer != null)
            onPicked(Answer);
    }
}

/// <summary>Gives every popup a recording canvas of its own, as windows would, inside a screen
/// <see cref="Area"/> in UI space.</summary>
public sealed class TestPopupHost : IPopupHost
{
    private readonly Dictionary<int, RecordingCanvas> _canvases = new();

    public UiRect Area { get; set; } = new(-200f, -200f, 800f, 700f);
    public Dictionary<int, UiRect> Shown { get; } = new();

    public RecordingCanvas CanvasOf(int id) => _canvases[id];

    public void BeginFrame()
    {
        Shown.Clear();
        foreach (var canvas in _canvases.Values)
            canvas.Reset();
    }

    public UiRect Place(UiRect preferred, UiRect fallback) => PopupPlacement.Fit(preferred, fallback, Area);

    public ICanvas Show(int id, UiRect rect, bool passThrough)
    {
        Shown[id] = rect;
        if (!_canvases.TryGetValue(id, out var canvas))
            _canvases[id] = canvas = new RecordingCanvas(new SyntheticTextMeasurer());
        return canvas;
    }
}
