using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>
/// The app's state and what the user can do to it. Lives on the UI thread: background work
/// (opening, decompiling, resolving, searching) hands results back through <see cref="Post"/>.
/// A result is dropped when the user asked for something else in the meantime.
/// </summary>
public sealed class Workspace
{
    private const int NoTicket = -1;

    private readonly AppSettings _settings;
    private readonly List<DocumentTab> _tabs = new();
    private Action<Action> _post = static _ => throw new InvalidOperationException("The workspace is not attached to a running app.");
    private int _intent;
    private int _resolving = NoTicket;
    private int _reload;
    private CancellationTokenSource? _reloadWork;
    private bool _focusSearch;

    public Workspace(AppSettings settings)
    {
        _settings = settings;
        DarkMode = settings.DarkMode;
        Language = settings.Language;
        ShowNonPublic = settings.ShowNonPublic;
        TreeWidth = settings.TreeWidth;
        SearchHeight = settings.SearchHeight;
        Tree = new BrowseTree(Post);
        Search = new SearchModel(ReportError);
        Picker = new FrameworkPickerModel(ReportError);
        if (settings.LoadError is { } error)
            ReportError("Cannot read the settings; starting with defaults", error);
    }

    public AssemblyList Assemblies { get; } = new();
    public BrowseTree Tree { get; }
    public NavigationHistory History { get; } = new();
    public SearchModel Search { get; }
    public FrameworkPickerModel Picker { get; }
    public ErrorLog Errors { get; } = new();
    public IReadOnlyList<DocumentTab> Tabs => _tabs;

    public bool DarkMode;
    public bool ShowNonPublic;
    public float TreeWidth;
    public float SearchHeight;

    public int ActiveIndex { get; private set; } = -1;

    public DecompilationLanguage Language { get; private set; }

    /// <summary>The node the tree's context menu was opened on.</summary>
    public BrowseNode? MenuTarget { get; private set; }

    public DocumentTab? ActiveTab => ActiveIndex >= 0 && ActiveIndex < _tabs.Count ? _tabs[ActiveIndex] : null;

    /// <summary>True while a reference or a history entry is being found in the background.</summary>
    public bool IsResolving => _resolving == _intent;

    /// <summary>True while <see cref="Refresh"/> reopens the assemblies.</summary>
    public bool IsReloading => _reloadWork != null;

    /// <summary>Connects the workspace to the running app: <paramref name="post"/> runs an action on
    /// the UI thread.</summary>
    public void Attach(Action<Action> post) => _post = post;

    public void Post(Action action) => _post(action);

    /// <summary>Called at the start of every frame.</summary>
    public void Sync()
    {
        if (Tree.Sync(Assemblies) && !IsReloading)
            SaveSettings();
    }

    /// <summary>Opens the files in the background, then shows the last one opened.</summary>
    public void Open(IEnumerable<string> paths)
    {
        var files = paths.ToArray();
        if (files.Length == 0) return;
        var ticket = Intend();
        RunInBackground(() => OpenFiles(files, CancellationToken.None), (opened, error) =>
        {
            if (error != null)
            {
                ReportError("Cannot open the assemblies", error.ToString());
                return;
            }
            ReportAll(opened!.Errors);
            if (opened.Last is { } last && ticket == _intent)
                Show(last.Root);
        });
    }

    public void Open(string path) => Open([path]);

    public void Close(LoadedAssembly assembly) => Assemblies.Close(assembly);

    public void ClearList()
    {
        Intend();
        StopReload();
        Assemblies.Clear();
        Tree.View.ClearSelection();
        Tree.View.CollapseAll();
        MenuTarget = null;
    }

    /// <summary>Reloads every assembly from disk in the background, then expands the tree as it was
    /// and shows each tab's place again, decompiled from the fresh files.</summary>
    public void Refresh()
    {
        var intent = Intend();
        StopReload();
        var reload = ++_reload;
        var work = new CancellationTokenSource();
        _reloadWork = work;

        var paths = Assemblies.FilePaths;
        var expanded = Tree.ExpandedPaths();
        var tabs = new List<(DocumentTab Tab, NodePath Location, int Version)>();
        foreach (var tab in _tabs)
        {
            if (tab.Location is not { } location) continue;
            tab.Release("Reloading…");
            tabs.Add((tab, location, tab.Version));
        }
        var places = tabs.ConvertAll(t => t.Location);

        Assemblies.Clear();
        Tree.View.CollapseAll();
        Tree.View.ClearSelection();
        MenuTarget = null;

        var token = work.Token;
        RunInBackground(() => Reload(paths, expanded, places, token), (reloaded, error) =>
        {
            work.Dispose();
            if (reload != _reload) return;
            _reloadWork = null;
            if (error != null)
            {
                ReportError("Cannot reload the assemblies", error.ToString());
                return;
            }
            ReportAll(reloaded!.Errors);
            Tree.Expand(reloaded.Expanded);
            for (var i = 0; i < tabs.Count; i++)
            {
                var (tab, location, version) = tabs[i];
                if (!_tabs.Contains(tab) || tab.Version != version) continue;
                if (reloaded.Places[i] is { } node)
                {
                    tab.Show(node, Language);
                    if (tab == ActiveTab && intent == _intent)
                        Tree.Reveal(node);
                }
                else
                {
                    tab.Release($"{location.Text} is gone after reloading.");
                    ReportError($"Cannot find {location.Text} after reloading", location.ToString());
                }
            }
            SaveSettings();
        });
    }

    public void SetLanguage(DecompilationLanguage language)
    {
        if (language == Language) return;
        Language = language;
        ActiveTab?.EnsureLanguage(language);
    }

    /// <summary>Shows <paramref name="node"/> in the active tab, or a new one, reveals it in the tree
    /// and records it in the history.</summary>
    public void Show(BrowseNode node, bool inNewTab = false)
    {
        if (node.Kind == NodeKind.Loading) return;
        Intend();
        ShowIn(inNewTab ? null : ActiveTab, node);
        History.Push(node.Path);
    }

    /// <summary>Resolves <paramref name="reference"/> in the background (it may load an assembly),
    /// then shows it in the tab it was asked from, or a new one.</summary>
    public void Navigate(CodeReference reference, bool inNewTab)
    {
        if (!reference.IsNavigable) return;
        var ticket = Intend();
        var origin = inNewTab ? null : ActiveTab;
        _resolving = ticket;
        RunInBackground(() => Assemblies.FindNode(reference), (node, error) =>
        {
            if (ticket != _intent) return;
            _resolving = NoTicket;
            if (node is null)
            {
                ReportError($"Cannot resolve {reference.Name}", error?.ToString() ?? $"No definition of {reference} was found in the open assemblies or their references.");
                return;
            }
            ShowIn(origin != null && _tabs.Contains(origin) ? origin : null, node);
            History.Push(node.Path);
        });
    }

    /// <summary>Goes where a node points (a base type, an assembly reference), if anywhere.</summary>
    public void GoToTarget(BrowseNode node)
    {
        if (node.Target is { } target)
            Navigate(target, inNewTab: false);
    }

    public void GoBack()
    {
        if (History.Back() is { } place)
            Restore(place);
    }

    public void GoForward()
    {
        if (History.Forward() is { } place)
            Restore(place);
    }

    public void Activate(int index)
    {
        if (index < 0 || index >= _tabs.Count) return;
        Intend();
        ActiveIndex = index;
        var tab = _tabs[index];
        tab.EnsureLanguage(Language);
        if (tab.Node is { } node && IsListed(node))
            Tree.Reveal(node);
    }

    public void CycleTab(int by)
    {
        if (_tabs.Count > 1)
            Activate(((ActiveIndex + by) % _tabs.Count + _tabs.Count) % _tabs.Count);
    }

    public void CloseTab(DocumentTab tab)
    {
        var index = _tabs.IndexOf(tab);
        if (index < 0) return;
        Intend();
        tab.Cancel();
        _tabs.RemoveAt(index);
        if (_tabs.Count == 0)
            ActiveIndex = -1;
        else if (index < ActiveIndex || ActiveIndex >= _tabs.Count)
            Activate(ActiveIndex - 1);
        else if (index == ActiveIndex)
            Activate(ActiveIndex);
    }

    public void SetMenuTarget(BrowseNode node) => MenuTarget = node;

    public void OpenSearch()
    {
        Search.IsOpen = true;
        _focusSearch = true;
    }

    /// <summary>True once after <see cref="OpenSearch"/>: the search box should take focus.</summary>
    public bool TakeSearchFocus()
    {
        var focus = _focusSearch;
        _focusSearch = false;
        return focus;
    }

    public void ReportError(string title, string? details = null) => Errors.Add(title, details);

    public void SaveSettings()
    {
        _settings.Assemblies = Assemblies.FilePaths.ToList();
        _settings.DarkMode = DarkMode;
        _settings.Language = Language;
        _settings.ShowNonPublic = ShowNonPublic;
        _settings.TreeWidth = TreeWidth;
        _settings.SearchHeight = SearchHeight;
        if (_settings.Save() is { } error)
            ReportError("Cannot save the settings", error);
    }

    private int Intend() => ++_intent;

    private bool IsListed(BrowseNode node) => Assemblies.Assemblies.Contains(node.Assembly);

    private void Restore(NodePath place)
    {
        var ticket = Intend();
        var tab = ActiveTab;
        if (tab is { Node: { } shown } && place.Equals(tab.Location) && IsListed(shown))
        {
            ShowIn(tab, shown);
            return;
        }
        _resolving = ticket;
        RunInBackground(() => Assemblies.FindNode(place), (node, error) =>
        {
            if (ticket != _intent) return;
            _resolving = NoTicket;
            if (node is null)
            {
                ReportError($"Cannot go back to {place.Text}", error?.ToString() ?? $"{place} is no longer there.");
                return;
            }
            ShowIn(tab != null && _tabs.Contains(tab) ? tab : null, node);
        });
    }

    private void ShowIn(DocumentTab? tab, BrowseNode node)
    {
        if (tab == null)
        {
            tab = new DocumentTab(Post, ReportError);
            _tabs.Add(tab);
        }
        ActiveIndex = _tabs.IndexOf(tab);
        tab.Show(node, Language);
        Tree.Reveal(node);
    }

    private void StopReload()
    {
        _reload++;
        _reloadWork?.Cancel();
        _reloadWork = null;
    }

    private void ReportAll(List<(string Title, string Details)> errors)
    {
        foreach (var (title, details) in errors)
            ReportError(title, details);
    }

    private void RunInBackground<T>(Func<T> work, Action<T?, Exception?> done) =>
        Task.Run(work).ContinueWith(task => Post(() =>
        {
            if (task.IsCompletedSuccessfully)
                done(task.Result, null);
            else if (!task.IsCanceled)
                done(default, task.Exception!.GetBaseException());
        }), TaskScheduler.Default);

    private sealed record Opened(LoadedAssembly? Last, List<(string Title, string Details)> Errors);

    private sealed record Reloaded(List<(string Title, string Details)> Errors, List<BrowseNode> Expanded, BrowseNode?[] Places);

    private Opened OpenFiles(IEnumerable<string> paths, CancellationToken cancellationToken)
    {
        LoadedAssembly? last = null;
        var errors = new List<(string, string)>();
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                last = Assemblies.Open(path);
            }
            catch (AssemblyLoadException e)
            {
                errors.Add(($"Cannot open {Path.GetFileName(path)}", e.InnerException?.ToString() ?? e.Message));
            }
        }
        return new Opened(last, errors);
    }

    private Reloaded Reload(IReadOnlyList<string> paths, List<NodePath> expanded, List<NodePath> places, CancellationToken cancellationToken)
    {
        var opened = OpenFiles(paths, cancellationToken);
        var nodes = new List<BrowseNode>();
        foreach (var path in expanded)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Assemblies.FindNode(path) is not { } node) continue;
            _ = node.Children;
            nodes.Add(node);
        }
        var found = new BrowseNode?[places.Count];
        for (var i = 0; i < found.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            found[i] = Assemblies.FindNode(places[i]);
        }
        return new Reloaded(opened.Errors, nodes, found);
    }
}
