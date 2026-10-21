using ZGF.Gui.Immediate.Widgets;
using System.Globalization;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Analysis;
using ZGF.ILSpy.Core.Metadata;
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
    private int _savedAnalyzer;
    private List<CodeAnchor>? _analyzed;
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
        _active = AddTab();
        Layout = DockPanels.Restore(settings.Layout, _active.Panel, out var placed);
        Tree = new BrowseTree(Post);
        Analyzer = new AnalyzerTree(Post, () => Assemblies);
        Search = new SearchModel(() => Strings.For(Locale), ReportError);
        Layout.NewPanel = () => AddTab().Panel;
        Layout.PanelClosed += panel =>
        {
            if (panel == DockPanels.Search)
                Search.Clear();
            RemoveTab(panel);
        };
        Bookmarks.Load(settings.Bookmarks);
        BookmarkColumns.Restore(settings.BookmarkColumns);
        _savedBookmarks = Bookmarks.Revision;
        Picker = new FrameworkPickerModel(() => Strings.For(Locale), ReportError);
        if (!placed)
            DockDocument(_active.Panel);
        _analyzed = settings.Analyzed is { Count: > 0 } analyzed ? analyzed : null;
        if (settings.LoadError is { } error)
            ReportError(Strings.For(Locale).ErrorReadSettings, error);
    }

    public AssemblyList Assemblies { get; } = new();
    public BrowseTree Tree { get; }
    public NavigationHistory History { get; } = new();
    public BookmarkList Bookmarks { get; } = new();

    /// <summary>The symbols being analyzed and what the analyses found.</summary>
    public AnalyzerTree Analyzer { get; }

    /// <summary>The columns of the bookmarks panel: mark, name, location and module.</summary>
    public TableColumns BookmarkColumns { get; } = new(new TableColumn(Fixed: 28f), new TableColumn(2f, 80f), new TableColumn(3f, 80f),
        new TableColumn(2f, 60f));
    public SearchModel Search { get; }
    public FrameworkPickerModel Picker { get; }
    public ErrorLog Errors { get; } = new();
    public IReadOnlyList<DocumentTab> Tabs => _tabs;

    public bool DarkMode;
    public bool ShowNonPublic;

    /// <summary>The interface language.</summary>
    public Locale Locale;

    /// <summary>Where the views and the open documents are docked, all as tabs: side by side,
    /// stacked or in floating windows.</summary>
    public DockLayout Layout { get; }

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
        if (_analyzed is { Count: > 0 } analyzed)
            RestoreAnalyzed(analyzed);
        if ((Tree.Sync(Assemblies) || Bookmarks.Revision != _savedBookmarks || Analyzer.Revision != _savedAnalyzer) && !IsReloading)
            SaveSettings();
    }

    /// <summary>Finds the symbols analyzed in the last run again in the background, opening their
    /// assemblies' files without listing them, and analyzes those still there, in their order.</summary>
    private void RestoreAnalyzed(List<CodeAnchor> analyzed)
    {
        RunInBackground(() => analyzed.ConvertAll(a => Analysis.Find(Assemblies, a)), (found, _) =>
        {
            _analyzed = null;
            foreach (var symbol in found ?? [])
            {
                if (symbol != null)
                    Analyzer.Analyze(symbol);
            }
            _savedAnalyzer = Analyzer.Revision;
        });
        _analyzed = [];
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
        Analyzer.Clear();
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
            var position = tab.Language != Language && tab.Grid == null ? null : tab.Position;
            tab.Release(Strings.For(Locale).DocumentReloading);
            tabs.Add((tab, location, tab.Version, position));
        }
        var places = tabs.ConvertAll(t => t.Location);

        Assemblies.Clear();
        Tree.View.CollapseAll();
        Tree.View.ClearSelection();
        Analyzer.Clear();
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
        foreach (var group in Layout.AllGroups())
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

    /// <summary>Shows the table row a metadata token leads to in the active tab, or a new one, and
    /// records it in the history.</summary>
    public void Show(MetadataTarget target, bool inNewTab)
    {
        Intend();
        LeaveActive();
        ShowIn(inNewTab ? null : ActiveTab, target.Node, row: target.Row);
        History.Push(target.Node, Language);
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
        var anchor = tab.Document.AnchorAt(line);
        Bookmarks.Add(new Bookmark(node.Path, tab.Language, line, anchor, Bookmarks.NextName(n => t.BookmarksDefaultName(n)), ILSpyTree.Describe(node, anchor)));
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

    /// <summary>Bookmarks read from a file, waiting for the user to say whether they replace the
    /// ones there are or merge in beside them.</summary>
    public List<Bookmark>? PendingImport { get; private set; }

    /// <summary>True while imported bookmarks' places are being found in the background.</summary>
    public bool IsImporting { get; private set; }

    public void ExportBookmarks(string path)
    {
        if (BookmarkFile.Write(path, Bookmarks.Save()) is { } error)
            ReportError(Strings.For(Locale).ErrorExportBookmarks, error);
    }

    /// <summary>Reads the bookmarks in the file at <paramref name="path"/> — this app's or the real
    /// ILSpy's — and finds the place of each in the background: takes them at once when there are
    /// none yet, and otherwise asks how through <see cref="PendingImport"/>. Bookmarks whose place
    /// is not found are left out and reported.</summary>
    public void ImportBookmarks(string path)
    {
        var t = Strings.For(Locale);
        var file = Path.GetFileName(path);
        if (BookmarkFile.Read(path, out var error) is not { } records)
        {
            ReportError(t.ErrorImportBookmarks(file), error);
            return;
        }
        IsImporting = true;
        RunInBackground(() => records.ConvertAll(r => (Record: r, Place: PlaceOf(r))), (found, failure) =>
        {
            IsImporting = false;
            if (failure != null)
            {
                ReportError(t.ErrorImportBookmarks(file), failure.ToString());
                return;
            }
            var lost = found!.FindAll(static f => f.Place == null);
            if (lost.Count > 0)
                ReportError(t.ErrorBookmarksNotFound(file), string.Join(Environment.NewLine,
                    lost.Select(static f => $"{f.Record.Name}: {f.Record.LocationNodeName ?? f.Record.MemberName} ({f.Record.FileName})")));
            var bookmarks = found.Where(static f => f.Place != null).Select(static f => Bookmark.FromRecord(f.Record, f.Place!)).ToList();
            if (Bookmarks.Items.Count == 0)
                Bookmarks.Import(bookmarks, BookmarkImportMode.Replace);
            else if (bookmarks.Count > 0)
                PendingImport = bookmarks;
        });
    }

    private NodePath? PlaceOf(BookmarkRecord record) =>
        record.Place is { } place ? NodePath.From(record.FileName, place)
        : record.ViewState?.SelectedTreeNodePath is { Count: > 0 } path && ILSpyTree.Find(Assemblies, path) is { } found ? found
        : ILSpyTree.FindEntity(Assemblies, record.FileName, record.Token);

    /// <summary>Imports the <see cref="PendingImport"/> as <paramref name="mode"/> says, or drops it when null.</summary>
    public void FinishImport(BookmarkImportMode? mode)
    {
        if (mode is { } how && PendingImport is { } saved)
            Bookmarks.Import(saved, how);
        PendingImport = null;
    }

    /// <summary>Analyzes the type or member <paramref name="node"/> stands for in the analyzer, which is
    /// opened if it is not.</summary>
    public void Analyze(BrowseNode node)
    {
        if (Analysis.Of(node) is not { } symbol) return;
        OpenAnalyzer();
        Analyzer.Analyze(symbol);
    }

    /// <summary>Finds what <paramref name="reference"/> points at in the background, opening its
    /// assembly if it is not, and analyzes it.</summary>
    public void Analyze(CodeReference reference)
    {
        RunInBackground(() => Assemblies.FindNode(reference), (node, error) =>
        {
            if (node != null)
            {
                Analyze(node);
                return;
            }
            var t = Strings.For(Locale);
            ReportError(t.ErrorResolve(reference.Name), error?.ToString() ?? t.ErrorNoDefinition(reference));
        });
    }

    /// <summary>Docks the analyzer beside the search results or bookmarks, or below the documents,
    /// if it is not docked, and brings it to the front.</summary>
    public void OpenAnalyzer()
    {
        if (Layout.Contains(DockPanels.Analyzer))
        {
        }
        else if ((Layout.GroupOf(DockPanels.Search) ?? Layout.GroupOf(DockPanels.Bookmarks)) is { } beside)
        {
            Layout.Dock(DockPanels.Analyzer, beside, DockEdge.Center);
        }
        else
        {
            DockBeside(DockPanels.Analyzer, DocumentGroup() ?? Layout.Root, DockEdge.Bottom, 0.3f);
        }
        Layout.Activate(DockPanels.Analyzer);
    }

    /// <summary>Docks the bookmarks panel if it is not, and brings it to the front.</summary>
    public void OpenBookmarks()
    {
        if (Layout.Contains(DockPanels.Bookmarks))
        {
        }
        else if (Layout.GroupOf(DockPanels.Search) is { } search)
        {
            Layout.Dock(DockPanels.Bookmarks, search, DockEdge.Center);
        }
        else
        {
            DockBeside(DockPanels.Bookmarks, DocumentGroup() ?? Layout.Root, DockEdge.Bottom, 0.3f);
        }
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
        Layout.Activate(tab.Panel);
        Focus(tab);
    }

    /// <summary>Called after the documents are recorded: makes the tab the user last pressed, docked
    /// or brought to front the active one.</summary>
    public void SyncActiveTab()
    {
        if (FindTab(Layout.Focused) is { } tab && tab != _active)
            Focus(tab);
    }

    /// <summary>Brings the next tab of the active tab's group to the front, or the previous one.</summary>
    public void CycleTab(int by)
    {
        if (_active == null || Layout.GroupOf(_active.Panel) is not { } group) return;
        var tabs = group.Panels.FindAll(p => FindTab(p) != null);
        if (tabs.Count < 2) return;
        var next = tabs[((tabs.IndexOf(_active.Panel) + by) % tabs.Count + tabs.Count) % tabs.Count];
        if (FindTab(next) is { } tab)
            Activate(tab);
    }

    public void CloseTab(DocumentTab tab) => Layout.Close(tab.Panel);

    /// <summary>Puts the layout back as it starts out: the assemblies left of every open document,
    /// tabbed together in one group, with the search results under them while a search is open.
    /// Floating windows and the other views close; the documents stay open.</summary>
    public void ResetLayout()
    {
        var documents = Layout.AllGroups().SelectMany(static g => g.Panels).Where(p => FindTab(p) != null).ToList();
        Layout.Windows.Clear();
        Layout.Root = new DockGroup { Id = Layout.NextId++, Panels = { DockPanels.Assemblies }, Active = DockPanels.Assemblies };
        if (documents.Count == 0)
        {
            _active = AddTab();
            documents.Add(_active.Panel);
        }
        DockBeside(documents[0], Layout.Root, DockEdge.Right, 0.66f);
        var group = Layout.GroupOf(documents[0])!;
        foreach (var panel in documents.Skip(1))
            Layout.Dock(panel, group, DockEdge.Center);
        if (_active != null && Layout.Contains(_active.Panel))
            Layout.Activate(_active.Panel);
        SyncSearchPanel();
    }

    /// <summary>Docks the assemblies view where it starts out, left of everything, if it is closed,
    /// and brings it to the front.</summary>
    public void OpenAssemblies()
    {
        if (!Layout.Contains(DockPanels.Assemblies))
            DockBeside(DockPanels.Assemblies, Layout.Root, DockEdge.Left, 0.34f);
        Layout.Activate(DockPanels.Assemblies);
    }

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
            DockBeside(DockPanels.Search, DocumentGroup() ?? Layout.Root, DockEdge.Bottom, 0.3f);
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
        if (_analyzed is not { Count: 0 })
            _settings.Analyzed = _analyzed ?? Analyzer.Roots.Select(static r => r.Symbol.Definition).OfType<CodeAnchor>().ToList();
        _savedAnalyzer = Analyzer.Revision;
        _settings.BookmarkColumns = BookmarkColumns.Weights.ToList();
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
        if ((FindTab(Layout.Focused) ?? FrontTab()) is { } next)
            Focus(next);
    }

    private DocumentTab? FrontTab()
    {
        foreach (var group in Layout.AllGroups())
        {
            if (FindTab(group.Active) is { } tab)
                return tab;
        }
        return null;
    }

    /// <summary>The group a new document joins: the active document's, else the first holding one.</summary>
    private DockGroup? DocumentGroup()
    {
        if (_active != null && Layout.GroupOf(_active.Panel) is { } active)
            return active;
        foreach (var group in Layout.AllGroups())
        {
            if (group.Panels.Exists(p => FindTab(p) != null))
                return group;
        }
        return null;
    }

    /// <summary>Docks a new document into the <see cref="DocumentGroup"/>, or where documents start
    /// out, right of everything, when there is none.</summary>
    private void DockDocument(string panel)
    {
        if (DocumentGroup() is { } group)
            Layout.Dock(panel, group, DockEdge.Center);
        else
            DockBeside(panel, Layout.Root, DockEdge.Right, 0.66f);
    }

    /// <summary>Docks <paramref name="panel"/> beside <paramref name="target"/>, or into it when the
    /// layout is empty.</summary>
    private void DockBeside(string panel, DockNode target, DockEdge edge, float share)
    {
        if (Layout.Root is DockGroup { Panels.Count: 0 } root)
            Layout.Dock(panel, root, DockEdge.Center);
        else
            Layout.Dock(panel, target, edge, share);
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
        if (ActiveTab is { Position: { } position } tab && History.Current is { } current && current.Place.Equals(tab.Location))
            History.Leave(tab.Language, position);
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

    private void ShowIn(DocumentTab? tab, BrowseNode node, CodeViewPosition? position = null, Bookmark? target = null, int row = MetadataGridState.None)
    {
        if (tab == null)
        {
            tab = AddTab();
            DockDocument(tab.Panel);
        }
        else
        {
            Layout.Activate(tab.Panel);
        }
        _active = tab;
        tab.Show(node, Language, position, target, row);
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
