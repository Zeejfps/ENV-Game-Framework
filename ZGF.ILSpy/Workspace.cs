using ZGF.Gui.Immediate.Widgets;
using System.Globalization;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

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
    private int _savedBookmarks;
    private CancellationTokenSource? _reloadWork;
    private bool _focusSearch;
    private DocumentTab? _active;

    public Workspace(AppSettings settings)
    {
        _settings = settings;
        DarkMode = settings.DarkMode;
        Language = settings.Language;
        Locale = settings.Locale ?? Locales.For(CultureInfo.CurrentUICulture);
        ShowNonPublic = settings.ShowNonPublic;
        Layout = DockPanels.Restore(settings.Layout);
        Tree = new BrowseTree(Post);
        Search = new SearchModel(() => Strings.For(Locale), ReportError);
        Layout.PanelClosed += panel =>
        {
            if (panel == DockPanels.Search)
                Search.Clear();
        };
        Bookmarks.Load(settings.Bookmarks);
        _savedBookmarks = Bookmarks.Revision;
        Picker = new FrameworkPickerModel(() => Strings.For(Locale), ReportError);
        Documents = new DockLayout { NewPanel = () => AddTab().Panel };
        Documents.PanelClosed += RemoveTab;
        if (settings.LoadError is { } error)
            ReportError(Strings.For(Locale).ErrorReadSettings, error);
    }

    public AssemblyList Assemblies { get; } = new();
    public BrowseTree Tree { get; }
    public NavigationHistory History { get; } = new();
    public BookmarkList Bookmarks { get; } = new();
    public SearchModel Search { get; }
    public FrameworkPickerModel Picker { get; }
    public ErrorLog Errors { get; } = new();
    public IReadOnlyList<DocumentTab> Tabs => _tabs;

    public bool DarkMode;
    public bool ShowNonPublic;

    /// <summary>The interface language.</summary>
    public Locale Locale;

    /// <summary>Where the tree, documents and search results are docked.</summary>
    public DockLayout Layout { get; }

    /// <summary>Where the open tabs are docked inside the documents area: side by side, stacked or
    /// in floating windows.</summary>
    public DockLayout Documents { get; }

    public DecompilationLanguage Language { get; private set; }

    /// <summary>The node the tree's context menu was opened on.</summary>
    public BrowseNode? MenuTarget { get; private set; }

    /// <summary>The tab the user last worked in, where the tree and navigation show what they pick.</summary>
    public DocumentTab? ActiveTab => _active;

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
        if ((Tree.Sync(Assemblies) || Bookmarks.Revision != _savedBookmarks) && !IsReloading)
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
                ReportError(Strings.For(Locale).ErrorOpenAssemblies, error.ToString());
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
        var tabs = new List<(DocumentTab Tab, NodePath Location, int Version, CodeViewPosition? Position)>();
        foreach (var tab in _tabs)
        {
            if (tab.Location is not { } location) continue;
            CodeViewPosition? position = tab.IsBusy || tab.Node == null || tab.Language != Language ? null : tab.View.Position;
            tab.Release(Strings.For(Locale).DocumentReloading);
            tabs.Add((tab, location, tab.Version, position));
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
                ReportError(Strings.For(Locale).ErrorReloadAssemblies, error.ToString());
                return;
            }
            ReportAll(reloaded!.Errors);
            Tree.Expand(reloaded.Expanded);
            for (var i = 0; i < tabs.Count; i++)
            {
                var (tab, location, version, position) = tabs[i];
                if (!_tabs.Contains(tab) || tab.Version != version) continue;
                if (reloaded.Places[i] is { } node)
                {
                    tab.Show(node, Language, position);
                    if (tab == ActiveTab && intent == _intent)
                        Tree.Reveal(node);
                }
                else
                {
                    var t = Strings.For(Locale);
                    tab.Release(t.DocumentGone(location.Text));
                    ReportError(t.ErrorFindAfterReload(location.Text), location.ToString());
                }
            }
            SaveSettings();
        });
    }

    public void SetLocale(Locale locale)
    {
        if (locale == Locale) return;
        Locale = locale;
        SaveSettings();
    }

    public void SetLanguage(DecompilationLanguage language)
    {
        if (language == Language) return;
        Language = language;
        foreach (var group in Documents.AllGroups())
            FindTab(group.Active)?.EnsureLanguage(language);
    }

    /// <summary>Shows <paramref name="node"/> in the active tab, or a new one, reveals it in the tree
    /// and records it in the history.</summary>
    public void Show(BrowseNode node, bool inNewTab = false)
    {
        if (node.Kind == NodeKind.Loading) return;
        Intend();
        LeaveActive();
        ShowIn(inNewTab ? null : ActiveTab, node);
        History.Push(node, Language);
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
                var t = Strings.For(Locale);
                ReportError(t.ErrorResolve(reference.Name), error?.ToString() ?? t.ErrorNoDefinition(reference));
                return;
            }
            LeaveActive();
            ShowIn(origin != null && _tabs.Contains(origin) ? origin : null, node);
            History.Push(node, Language);
        });
    }

    /// <summary>Goes where a node points (a base type, an assembly reference), if anywhere.</summary>
    public void GoToTarget(BrowseNode node)
    {
        if (node.Target is { } target)
            Navigate(target, inNewTab: false);
    }

    public void GoBack() => GoTo(History.CurrentIndex - 1);

    public void GoForward() => GoTo(History.CurrentIndex + 1);

    /// <summary>Goes to the history entry at <paramref name="index"/>, however far back or forward.</summary>
    public void GoTo(int index)
    {
        if (index < 0 || index >= History.Entries.Count || index == History.CurrentIndex) return;
        LeaveActive();
        Restore(History.MoveTo(index)!.Value);
    }

    /// <summary>Removes the bookmark marking <paramref name="line"/> of <paramref name="tab"/>'s
    /// document, or bookmarks the line, naming the bookmark by the statement or definition on it
    /// when it has one.</summary>
    public void ToggleBookmark(DocumentTab tab, int line)
    {
        if (tab is not { IsBusy: false, Node: { } node } || tab.Document.IsError || line < 0 || line >= tab.Document.Lines.Length) return;
        if (Bookmarks.At(tab.Document, node.Path, line) is { } existing)
        {
            Bookmarks.Remove(existing);
            return;
        }
        var t = Strings.For(Locale);
        Bookmarks.Add(new Bookmark(node.Path, tab.Language, line, tab.Document.AnchorAt(line), Bookmarks.NextName(n => t.BookmarksDefaultName(n))));
    }

    /// <summary>Shows the line <paramref name="bookmark"/> marks in the active tab, opening its
    /// assembly first if it is not open, and records it in the history.</summary>
    public void GoTo(Bookmark bookmark)
    {
        var ticket = Intend();
        var tab = ActiveTab;
        var place = bookmark.Place;
        _resolving = ticket;
        RunInBackground(() => Assemblies.FindNode(place), (node, error) =>
        {
            if (ticket != _intent) return;
            _resolving = NoTicket;
            if (node is null)
            {
                var t = Strings.For(Locale);
                ReportError(t.ErrorGoToBookmark(bookmark.Name), error?.ToString() ?? t.ErrorNoLongerThere(place));
                return;
            }
            LeaveActive();
            ShowIn(tab != null && _tabs.Contains(tab) ? tab : null, node, target: bookmark);
            History.Push(node, Language);
        });
    }

    /// <summary>Goes to the next enabled bookmark after the selected one, or the previous when
    /// <paramref name="direction"/> is negative.</summary>
    public void StepBookmark(int direction)
    {
        if (Bookmarks.Step(direction) is { } bookmark)
            GoTo(bookmark);
    }

    /// <summary>Docks the bookmarks panel if it is not, and brings it to the front.</summary>
    public void OpenBookmarks()
    {
        if (!Layout.Contains(DockPanels.Bookmarks))
            Layout.Dock(DockPanels.Bookmarks, Layout.GroupOf(DockPanels.Search) ?? Layout.GroupOf(DockPanels.Documents) ?? Layout.Root,
                Layout.Contains(DockPanels.Search) ? DockEdge.Center : DockEdge.Bottom, 0.3f);
        Layout.Activate(DockPanels.Bookmarks);
    }

    /// <summary>The tab whose panel is <paramref name="panel"/>.</summary>
    public DocumentTab? FindTab(string? panel)
    {
        foreach (var tab in _tabs)
        {
            if (tab.Panel == panel)
                return tab;
        }
        return null;
    }

    public void Activate(DocumentTab tab)
    {
        if (!_tabs.Contains(tab)) return;
        Documents.Activate(tab.Panel);
        Focus(tab);
    }

    /// <summary>Called after the documents are recorded: makes the tab the user last pressed, docked
    /// or brought to front the active one.</summary>
    public void SyncActiveTab()
    {
        if (FindTab(Documents.Focused) is { } tab && tab != _active)
            Focus(tab);
    }

    /// <summary>Brings the next tab of the active tab's group to the front, or the previous one.</summary>
    public void CycleTab(int by)
    {
        if (_active == null || Documents.GroupOf(_active.Panel) is not { Panels.Count: > 1 } group) return;
        var count = group.Panels.Count;
        var next = group.Panels[((group.Panels.IndexOf(_active.Panel) + by) % count + count) % count];
        if (FindTab(next) is { } tab)
            Activate(tab);
    }

    public void CloseTab(DocumentTab tab) => Documents.Close(tab.Panel);

    public void SetMenuTarget(BrowseNode node) => MenuTarget = node;

    public void OpenSearch()
    {
        Search.IsOpen = true;
        _focusSearch = true;
        SyncSearchPanel();
        Layout.Activate(DockPanels.Search);
    }

    /// <summary>Docks the search results while a search is open and closes them once it is not.</summary>
    public void SyncSearchPanel()
    {
        if (Search.IsOpen && !Layout.Contains(DockPanels.Search))
            Layout.Dock(DockPanels.Search, Layout.GroupOf(DockPanels.Documents) ?? Layout.Root, DockEdge.Bottom, 0.3f);
        else if (!Search.IsOpen && Layout.Contains(DockPanels.Search))
            Layout.Close(DockPanels.Search);
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
        _settings.Locale = Locale;
        _settings.Layout = Layout;
        _settings.Bookmarks = Bookmarks.Save();
        _savedBookmarks = Bookmarks.Revision;
        if (_settings.Save() is { } error)
            ReportError(Strings.For(Locale).ErrorSaveSettings, error);
    }

    private int Intend() => ++_intent;

    private bool IsListed(BrowseNode node) => Assemblies.Assemblies.Contains(node.Assembly);

    private DocumentTab AddTab()
    {
        var tab = new DocumentTab(Post, () => Strings.For(Locale), ReportError);
        _tabs.Add(tab);
        return tab;
    }

    private void RemoveTab(string panel)
    {
        if (FindTab(panel) is not { } tab) return;
        Intend();
        tab.Cancel();
        _tabs.Remove(tab);
        if (tab != _active) return;
        _active = null;
        if ((FindTab(Documents.Focused) ?? FrontTab()) is { } next)
            Focus(next);
    }

    private DocumentTab? FrontTab()
    {
        foreach (var group in Documents.AllGroups())
        {
            if (FindTab(group.Active) is { } tab)
                return tab;
        }
        return null;
    }

    private void Focus(DocumentTab tab)
    {
        Intend();
        _active = tab;
        tab.EnsureLanguage(Language);
        if (tab.Node is { } node && IsListed(node))
            Tree.Reveal(node);
    }

    private void LeaveActive()
    {
        if (ActiveTab is { IsBusy: false, Node: not null } tab && History.Current is { } current && current.Place.Equals(tab.Location))
            History.Leave(tab.Language, tab.View.Position);
    }

    private void Restore(HistoryEntry entry)
    {
        var ticket = Intend();
        var tab = ActiveTab;
        var place = entry.Place;
        var position = entry.Language == Language ? entry.Position : null;
        if (tab is { Node: { } shown } && place.Equals(tab.Location) && IsListed(shown))
        {
            ShowIn(tab, shown, position);
            return;
        }
        _resolving = ticket;
        RunInBackground(() => Assemblies.FindNode(place), (node, error) =>
        {
            if (ticket != _intent) return;
            _resolving = NoTicket;
            if (node is null)
            {
                var t = Strings.For(Locale);
                ReportError(t.ErrorGoBack(place.Text), error?.ToString() ?? t.ErrorNoLongerThere(place));
                return;
            }
            ShowIn(tab != null && _tabs.Contains(tab) ? tab : null, node, position);
        });
    }

    private void ShowIn(DocumentTab? tab, BrowseNode node, CodeViewPosition? position = null, Bookmark? target = null)
    {
        if (tab == null)
        {
            var group = (_active != null ? Documents.GroupOf(_active.Panel) : null) ?? Documents.AllGroups().First();
            tab = AddTab();
            Documents.Dock(tab.Panel, group, DockEdge.Center);
        }
        else
        {
            Documents.Activate(tab.Panel);
        }
        _active = tab;
        tab.Show(node, Language, position, target);
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
                errors.Add((Strings.For(Locale).ErrorOpenFile(Path.GetFileName(path)), e.InnerException?.ToString() ?? e.Message));
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
