using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>
/// The app's state and what the user can do to it. Lives on the UI thread: background work
/// (decompiling, resolving references, searching) hands results back through <see cref="Post"/>.
/// </summary>
public sealed class Workspace
{
    private readonly AppSettings _settings;
    private Action<Action> _post = static _ => throw new InvalidOperationException("The workspace is not attached to a running app.");
    private int _navigation;
    private BrowseNode? _pendingShow;

    public Workspace(AppSettings settings)
    {
        _settings = settings;
        DarkMode = settings.DarkMode;
        Language = settings.Language;
        ShowNonPublic = settings.ShowNonPublic;
        TreeWidth = settings.TreeWidth;
        SearchHeight = settings.SearchHeight;
    }

    public AssemblyList Assemblies { get; } = new();
    public BrowseTree Tree { get; } = new();
    public NavigationHistory History { get; } = new();
    public SearchModel Search { get; } = new();
    public FrameworkPickerModel Picker { get; } = new();
    public List<DocumentTab> Tabs { get; } = new();

    public bool DarkMode;
    public bool ShowNonPublic;
    public float TreeWidth;
    public float SearchHeight;
    public int ActiveIndex = -1;
    public bool FocusSearch;

    public DecompilationLanguage Language { get; private set; }

    /// <summary>The last thing worth telling the user, e.g. a file that could not be opened.</summary>
    public string? Message { get; private set; }

    /// <summary>The node the tree's context menu was opened on.</summary>
    public BrowseNode? MenuTarget;

    public DocumentTab? ActiveTab => ActiveIndex >= 0 && ActiveIndex < Tabs.Count ? Tabs[ActiveIndex] : null;

    /// <summary>True while a reference is being resolved in the background.</summary>
    public bool IsResolving { get; private set; }

    /// <summary>Connects the workspace to the running app: <paramref name="post"/> runs an action on
    /// the UI thread.</summary>
    public void Attach(Action<Action> post) => _post = post;

    public void Post(Action action) => _post(action);

    /// <summary>Called at the start of every frame.</summary>
    public void Sync()
    {
        if (Tree.Sync(Assemblies))
            SaveSettings();
        if (_pendingShow is { } node)
        {
            _pendingShow = null;
            Tree.Reveal(node);
            Show(node);
        }
    }

    /// <summary>Opens the files and, from the next frame, shows the last one opened.</summary>
    public void Open(IEnumerable<string> paths)
    {
        if (OpenAll(paths) is { } last)
            _pendingShow = last.Root;
    }

    public void Open(string path) => Open([path]);

    public void Close(LoadedAssembly assembly) => Assemblies.Close(assembly);

    public void ClearList()
    {
        Assemblies.Clear();
        Tree.View.ClearSelection();
        Tree.View.CollapseAll();
    }

    /// <summary>Reloads every assembly from disk; tabs showing entities are re-resolved against the
    /// fresh files and decompiled again.</summary>
    public void Refresh()
    {
        var paths = Assemblies.FilePaths;
        var stale = Tabs.Select(t => (Tab: t, t.Node)).Where(t => t.Node != null).ToArray();
        Assemblies.Clear();
        Tree.View.CollapseAll();
        Tree.View.ClearSelection();
        OpenAll(paths);

        var language = Language;
        foreach (var (tab, node) in stale)
        {
            var reference = node!.Reference;
            var path = node.Assembly.FilePath;
            var isRoot = node.Kind == NodeKind.Assembly;
            Task.Run(() => reference != null ? Assemblies.FindNode(reference) : isRoot ? FindRoot(path) : null)
                .ContinueWith(task => Post(() =>
                {
                    if (task.IsCompletedSuccessfully && task.Result is { } fresh && Tabs.Contains(tab))
                    {
                        tab.Show(fresh, language, -1, Post);
                        if (tab == ActiveTab)
                            Tree.Reveal(fresh);
                    }
                }), TaskScheduler.Default);
        }
    }

    public void SetLanguage(DecompilationLanguage language)
    {
        if (language == Language) return;
        Language = language;
        ActiveTab?.EnsureLanguage(language, Post);
    }

    /// <summary>Shows <paramref name="node"/> in the active tab, or a new one, and records it in the
    /// history.</summary>
    public void Show(BrowseNode node, bool inNewTab = false, int line = -1)
    {
        ShowWithoutHistory(node, inNewTab, line);
        History.Push(new HistoryEntry(node, line));
    }

    /// <summary>Resolves <paramref name="reference"/> in the background (it may load an assembly),
    /// then reveals and shows it.</summary>
    public void Navigate(CodeReference reference, bool inNewTab)
    {
        if (!reference.IsNavigable) return;
        var ticket = ++_navigation;
        IsResolving = true;
        Message = null;
        Task.Run(() => Assemblies.FindNode(reference)).ContinueWith(task => Post(() =>
        {
            if (ticket != _navigation) return;
            IsResolving = false;
            if (!task.IsCompletedSuccessfully || task.Result is not { } node)
            {
                Message = $"Cannot resolve {reference.Name}.";
                return;
            }
            Tree.Reveal(node);
            Show(node, inNewTab);
        }), TaskScheduler.Default);
    }

    /// <summary>Goes where a node points (a base type, an assembly reference), if anywhere.</summary>
    public void GoToTarget(BrowseNode node)
    {
        if (node.Target is { } target)
            Navigate(target, inNewTab: false);
    }

    public void GoBack()
    {
        if (History.Back() is { } entry)
            Restore(entry);
    }

    public void GoForward()
    {
        if (History.Forward() is { } entry)
            Restore(entry);
    }

    public void Activate(int index)
    {
        if (index < 0 || index >= Tabs.Count) return;
        ActiveIndex = index;
        var tab = Tabs[index];
        tab.EnsureLanguage(Language, Post);
        if (tab.Node != null)
            Tree.Reveal(tab.Node);
    }

    public void CycleTab(int by)
    {
        if (Tabs.Count > 1)
            Activate(((ActiveIndex + by) % Tabs.Count + Tabs.Count) % Tabs.Count);
    }

    public void CloseTab(DocumentTab tab)
    {
        var index = Tabs.IndexOf(tab);
        if (index < 0) return;
        tab.Cancel();
        Tabs.RemoveAt(index);
        if (Tabs.Count == 0)
            ActiveIndex = -1;
        else if (index < ActiveIndex || ActiveIndex >= Tabs.Count)
            Activate(ActiveIndex - 1);
        else if (index == ActiveIndex)
            Activate(ActiveIndex);
    }

    public void OpenSearch()
    {
        Search.IsOpen = true;
        FocusSearch = true;
    }

    public void SaveSettings()
    {
        _settings.Assemblies = Assemblies.FilePaths.ToList();
        _settings.DarkMode = DarkMode;
        _settings.Language = Language;
        _settings.ShowNonPublic = ShowNonPublic;
        _settings.TreeWidth = TreeWidth;
        _settings.SearchHeight = SearchHeight;
        _settings.Save();
    }

    private void Restore(HistoryEntry entry)
    {
        Tree.Reveal(entry.Node);
        ShowWithoutHistory(entry.Node, false, entry.Line);
    }

    private void ShowWithoutHistory(BrowseNode node, bool inNewTab, int line)
    {
        if (inNewTab || ActiveTab == null)
        {
            Tabs.Add(new DocumentTab());
            ActiveIndex = Tabs.Count - 1;
        }
        ActiveTab!.Show(node, Language, line, Post);
    }

    private LoadedAssembly? OpenAll(IEnumerable<string> paths)
    {
        LoadedAssembly? last = null;
        foreach (var path in paths)
        {
            try
            {
                last = Assemblies.Open(path);
            }
            catch (AssemblyLoadException e)
            {
                Message = e.Message;
            }
        }
        return last;
    }

    private BrowseNode? FindRoot(string path)
    {
        foreach (var assembly in Assemblies.Assemblies)
        {
            if (string.Equals(assembly.FilePath, path, StringComparison.OrdinalIgnoreCase))
                return assembly.Root;
        }
        return null;
    }
}
