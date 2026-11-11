using ZGF.Desktop;
using ZGF.Gui.Hosting;

namespace ZGF.Gui.Immediate;

public enum FilePickerKind
{
    Folder,
    File,
    SaveFile,
}

/// <summary>A file picker the app opened in automation mode, and the path it was answered with, or null when
/// it was cancelled.</summary>
public sealed record FilePickerRequest(
    FilePickerKind Kind,
    string Title,
    string? InitialDirectory,
    string? SuggestedFileName,
    IReadOnlyList<FileFilter>? Filters,
    string? Answer);

/// <summary>An item of the system menu bar, under the titles of the menus that lead to it.</summary>
public sealed record SystemMenuItem(IReadOnlyList<string> Path, bool Enabled, bool Checked);

/// <summary>The clipboard of an app in automation mode, which only the app and its driver see. UI thread only.</summary>
internal sealed class AutomationClipboard : IClipboard
{
    public string? Text { get; private set; }
    public string? Html { get; private set; }

    public void SetText(string text)
    {
        Text = text;
        Html = null;
    }

    public void SetHtml(string html, string text)
    {
        Text = text;
        Html = html;
    }

    public string? GetText() => Text;

    public ClipboardImage? GetImage() => null;
}

/// <summary>Answers each picker the app opens with the next answer queued, a path or a cancel; with none
/// queued it cancels. UI thread only.</summary>
internal sealed class AutomationFilePicker : IFilePicker
{
    private readonly Queue<string?> _answers = new();
    private readonly List<FilePickerRequest> _requests = new();

    public IReadOnlyList<FilePickerRequest> Requests => _requests;

    public void Queue(string? answer) => _answers.Enqueue(answer);

    public void PickFolder(string title, string? initialDirectory, Action<string> onPicked) =>
        Pick(FilePickerKind.Folder, title, initialDirectory, null, null, onPicked, null);

    public void PickFile(string title, string? initialDirectory, IReadOnlyList<FileFilter>? filters, Action<string> onPicked) =>
        Pick(FilePickerKind.File, title, initialDirectory, null, filters, onPicked, null);

    public void PickSaveFile(string title, string? initialDirectory, string? suggestedFileName, IReadOnlyList<FileFilter>? filters, Action<string> onPicked,
        Action onCancelled) =>
        Pick(FilePickerKind.SaveFile, title, initialDirectory, suggestedFileName, filters, onPicked, onCancelled);

    private void Pick(FilePickerKind kind, string title, string? initialDirectory, string? suggestedFileName,
        IReadOnlyList<FileFilter>? filters, Action<string> onPicked, Action? onCancelled)
    {
        _answers.TryDequeue(out var answer);
        _requests.Add(new FilePickerRequest(kind, title, initialDirectory, suggestedFileName, filters, answer));
        if (answer != null)
            onPicked(answer);
        else
            onCancelled?.Invoke();
    }
}

/// <summary>Records each file the app shares (<see cref="AutomationEffect.FileShared"/>) and answers that it was
/// shared, as a user who picked an app would. UI thread only.</summary>
internal sealed class AutomationFileSharer(ImmediateAutomation automation) : IFileSharer
{
    public bool CanShare => true;

    public void Share(FileToShare file, Action<ShareOutcome> onDone)
    {
        automation.Record(new AutomationEffect.FileShared(file.Path, file.Title, file.Text));
        onDone(new ShareOutcome.Shared());
    }
}

/// <summary>Stands in for macOS's menu bar in automation mode: keeps the menus last shown, and picks their
/// items as the user would. UI thread only.</summary>
internal sealed class AutomationMenuBar : ISystemMenuBar
{
    private IReadOnlyList<AppMenu> _menus = [];
    private Action<AppMenuItem>? _picked;

    public void Show(IReadOnlyList<AppMenu> menus, Action<AppMenuItem> picked)
    {
        _menus = menus;
        _picked = picked;
    }

    public IReadOnlyList<SystemMenuItem> Items()
    {
        var items = new List<SystemMenuItem>();
        var path = new List<string>();
        foreach (var menu in _menus)
            Collect(menu, path, items);
        return items;
    }

    /// <summary>Picks the item <paramref name="path"/> names: the titles of the menus leading to it, then its own.</summary>
    /// <exception cref="ArgumentException">No item, or more than one, is there.</exception>
    public void Pick(IReadOnlyList<string> path)
    {
        var matches = new List<AppMenuItem>();
        foreach (var menu in _menus)
            Find(menu, path, 0, matches);
        if (matches.Count != 1 || _picked == null)
        {
            var found = matches.Count == 0 ? "no item" : $"{matches.Count} items";
            throw new ArgumentException($"The system menu bar has {found} at {string.Join(" > ", path)}.", nameof(path));
        }
        _picked(matches[0]);
    }

    private static void Collect(AppMenu menu, List<string> path, List<SystemMenuItem> into)
    {
        path.Add(menu.Title);
        foreach (var entry in menu.Entries)
        {
            if (entry is AppMenu submenu)
                Collect(submenu, path, into);
            else if (entry is AppMenuItem item)
                into.Add(new SystemMenuItem([..path, item.Title], item.IsEnabled, item.IsChecked));
        }
        path.RemoveAt(path.Count - 1);
    }

    private static void Find(AppMenu menu, IReadOnlyList<string> path, int depth, List<AppMenuItem> into)
    {
        if (depth >= path.Count - 1 || menu.Title != path[depth])
            return;
        foreach (var entry in menu.Entries)
        {
            if (entry is AppMenu submenu)
                Find(submenu, path, depth + 1, into);
            else if (entry is AppMenuItem item && depth == path.Count - 2 && item.Title == path[^1])
                into.Add(item);
        }
    }
}
