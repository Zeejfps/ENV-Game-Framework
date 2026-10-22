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
    private const string NamespaceScope = "innamespace";
    private const string AssemblyScope = "inassembly";

    private readonly AppSettings _settings;
    private Action<Action> _post = ThrowNotAttached;
    private readonly MainLayout _mainLayout;
    private List<CodeAnchor>? _analyzedToRestore;
    private bool _isRestoringAnalyzed;
    private bool _focusSearch;

    public AssemblyList Assemblies { get; } = new();
    public BrowseTree Tree { get; }
    public NavigationHistory History { get; } = new();
    public UserIntent Intent { get; } = new();
    public Reload Reload { get; } = new();
    public LastSave LastSave { get; } = new();
    public BookmarkList Bookmarks { get; } = new();

    /// <summary>The symbols being analyzed and what the analyses found.</summary>
    public AnalyzerTree Analyzer { get; }

    /// <summary>The columns of the bookmarks panel: mark, name, location and module.</summary>
    public TableColumns BookmarkColumns { get; } = new(
        new TableColumn(Fixed: 28f),
        new TableColumn(2f, 80f),
        new TableColumn(3f, 80f),
        new TableColumn(2f, 60f));

    /// <summary>The columns of the search results: name, location and assembly.</summary>
    public TableColumns SearchColumns { get; } = new(
        new TableColumn(3f, 120f),
        new TableColumn(3f, 80f),
        new TableColumn(2f, 60f));

    public SearchModel Search { get; }

    /// <summary>The metadata grids' flags filters.</summary>
    public MetadataFilters MetadataFilters { get; }
    public FrameworkPickerModel Picker { get; }
    public ErrorLog Errors { get; } = new();
    public DocumentTabs Tabs { get; } = new();

    public AppThemeId Theme;
    public bool ShowNonPublic;

    /// <summary>The interface language.</summary>
    public Locale Locale;

    /// <summary>Where the views and the open documents are docked, all as tabs: side by side,
    /// stacked or in floating windows.</summary>
    public DockLayout Layout => _mainLayout.Dock;

    public DecompilationLanguage Language { get; private set; }

    /// <summary>How C# is decompiled.</summary>
    public DecompilerOptions DecompilerOptions { get; private set; }

    /// <summary>The decompiler options panel's folded groups.</summary>
    public HashSet<string> FoldedOptionGroups { get; }

    /// <summary>The node the tree's context menu was opened on.</summary>
    public BrowseNode? MenuTarget { get; private set; }

    /// <summary>True while a reference or a history entry is being found in the background.</summary>
    public bool IsResolving => Intent.IsResolving;

    /// <summary>True while <see cref="Refresh"/> reopens the assemblies.</summary>
    public bool IsReloading => Reload.IsRunning;

    /// <summary>Bookmarks read from a file, waiting for the user to say whether they replace the
    /// ones there are or merge in beside them.</summary>
    public List<Bookmark>? PendingImport { get; set; }

    /// <summary>True while imported bookmarks' places are being found in the background.</summary>
    public bool IsImporting { get; set; }

    /// <summary>Set while the user is asked whether to reset the layout.</summary>
    public bool ConfirmingLayoutReset { get; private set; }

    public Workspace(AppSettings settings)
    {
        _settings = settings;
        Theme = settings.Theme;
        Language = settings.Language;
        DecompilerOptions = settings.Decompiler;
        FoldedOptionGroups = LoadFoldedOptionGroups(settings);
        Locale = ChooseLocale(settings);
        ShowNonPublic = settings.ShowNonPublic;
        var first = AddTab();
        Tabs.MakeActive(first);
        var dock = DockPanels.Restore(settings.Layout, first.Panel, out var placed);
        _mainLayout = new MainLayout(dock);
        Layout.NewPanel = () => AddTab().Panel;
        Layout.PanelClosed += OnPanelClosed;
        Tree = new BrowseTree(Post);
        Analyzer = new AnalyzerTree(Post, () => Assemblies);
        Search = new SearchModel(GetStrings, ReportError);
        Search.Kinds = settings.SearchKinds;
        MetadataFilters = new MetadataFilters(settings.FlagsFilters);
        Bookmarks.Load(settings.Bookmarks);
        BookmarkColumns.Restore(settings.BookmarkColumns);
        SearchColumns.Restore(settings.SearchColumns);
        LastSave.Record(Bookmarks, Analyzer, MetadataFilters);
        Picker = new FrameworkPickerModel(GetStrings, ReportError);
        if (!placed)
            _mainLayout.DockDocument(first.Panel, first.Panel);
        _analyzedToRestore = LoadAnalyzedToRestore(settings);
        ReportSettingsLoadError(settings);
    }

    private static HashSet<string> LoadFoldedOptionGroups(AppSettings settings)
    {
        if (settings.FoldedOptionGroups == null)
            return new HashSet<string>();
        return new HashSet<string>(settings.FoldedOptionGroups);
    }

    private static Locale ChooseLocale(AppSettings settings)
    {
        if (settings.Locale != null)
            return settings.Locale.Value;
        return Locales.For(CultureInfo.CurrentUICulture);
    }

    private void OnPanelClosed(string panel)
    {
        if (panel == DockPanels.Search)
            Search.Clear();
        RemoveTab(panel);
    }

    private static List<CodeAnchor>? LoadAnalyzedToRestore(AppSettings settings)
    {
        if (settings.Analyzed == null)
            return null;
        if (settings.Analyzed.Count == 0)
            return null;
        return settings.Analyzed;
    }

    private void ReportSettingsLoadError(AppSettings settings)
    {
        if (settings.LoadError == null)
            return;
        ReportError(GetStrings().ErrorReadSettings, settings.LoadError);
    }

    /// <summary>Connects the workspace to the running app: <paramref name="post"/> runs an action on
    /// the UI thread.</summary>
    public void Attach(Action<Action> post) => _post = post;

    public void Post(Action action) => _post(action);

    private static void ThrowNotAttached(Action action)
    {
        throw new InvalidOperationException("The workspace is not attached to a running app.");
    }

    /// <summary>Called at the start of every frame.</summary>
    public void Sync()
    {
        if (_analyzedToRestore != null)
            RestoreAnalyzed(_analyzedToRestore);
        MetadataFilters.Sync();
        var treeChanged = Tree.Sync(Assemblies);
        if (IsReloading)
            return;
        if (!treeChanged && !LastSave.IsBehind(Bookmarks, Analyzer, MetadataFilters))
            return;
        SaveSettings();
    }

    /// <summary>Finds the symbols analyzed in the last run again in the background, opening their
    /// assemblies' files without listing them, and analyzes those still there, in their order.</summary>
    private void RestoreAnalyzed(List<CodeAnchor> analyzed)
    {
        _analyzedToRestore = null;
        _isRestoringAnalyzed = true;
        RunInBackground(() => FindAnalyzed(analyzed), (found, _) => OnAnalyzedFound(found));
    }

    private List<AnalysisSymbol?> FindAnalyzed(List<CodeAnchor> analyzed)
    {
        return analyzed.ConvertAll(a => Analysis.Find(Assemblies, a));
    }

    private void OnAnalyzedFound(List<AnalysisSymbol?>? found)
    {
        _isRestoringAnalyzed = false;
        if (found != null)
            AnalyzeAll(found);
        LastSave.RecordAnalyzer(Analyzer);
    }

    private void AnalyzeAll(List<AnalysisSymbol?> symbols)
    {
        foreach (var symbol in symbols)
        {
            if (symbol != null)
                Analyzer.Analyze(symbol);
        }
    }

    /// <summary>Opens the files in the background, then shows the last one opened.</summary>
    public void Open(IEnumerable<string> paths)
    {
        var files = paths.ToArray();
        if (files.Length == 0)
            return;
        OpenInBackground(files);
    }

    private void OpenInBackground(string[] files)
    {
        var ticket = Intent.Begin();
        RunInBackground(() => OpenFiles(files, CancellationToken.None), (opened, error) => OnFilesOpened(opened, error, ticket));
    }

    private void OnFilesOpened(Opened? opened, Exception? error, int ticket)
    {
        if (error != null)
        {
            ReportError(GetStrings().ErrorOpenAssemblies, error.ToString());
            return;
        }
        ReportAll(opened!.Errors);
        if (opened.Last == null)
            return;
        if (Intent.IsStale(ticket))
            return;
        this.Show(opened.Last.Root);
    }

    public void Open(string path) => Open([path]);

    public void Close(LoadedAssembly assembly) => Assemblies.Close(assembly);

    public void ClearList()
    {
        Intent.Begin();
        Reload.Stop();
        ClearAssemblies();
    }

    private void ClearAssemblies()
    {
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
        var intent = Intent.Begin();
        var ticket = Reload.Start();

        var paths = Assemblies.FilePaths;
        var expanded = Tree.ExpandedPaths();
        var tabs = ReleaseShownTabs();
        var places = tabs.ConvertAll(t => t.Location);
        ClearAssemblies();

        var request = new ReloadRequest(ticket, intent, tabs);
        RunInBackground(() => ReopenFiles(paths, expanded, places, ticket.Token), (reloaded, error) => OnReloaded(reloaded, error, request));
    }

    private List<ReloadingTab> ReleaseShownTabs()
    {
        var reloading = new List<ReloadingTab>();
        foreach (var tab in Tabs)
        {
            var location = tab.Location;
            if (location == null)
                continue;
            var position = FindPositionToKeep(tab);
            tab.Release(GetStrings().DocumentReloading);
            reloading.Add(new ReloadingTab(tab, location, tab.Version, position));
        }
        return reloading;
    }

    private CodeViewPosition? FindPositionToKeep(DocumentTab tab)
    {
        if (tab.Grid != null)
            return tab.Position;
        if (tab.Language != Language)
            return null;
        return tab.Position;
    }

    private Reloaded ReopenFiles(IReadOnlyList<string> paths, List<NodePath> expanded, List<NodePath> places, CancellationToken cancellationToken)
    {
        var opened = OpenFiles(paths, cancellationToken);
        var expandedNodes = LoadExpandedNodes(expanded, cancellationToken);
        var placeNodes = FindNodes(places, cancellationToken);
        return new Reloaded(opened.Errors, expandedNodes, placeNodes);
    }

    private List<BrowseNode> LoadExpandedNodes(List<NodePath> expanded, CancellationToken cancellationToken)
    {
        var nodes = new List<BrowseNode>();
        foreach (var path in expanded)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var node = Assemblies.FindNode(path);
            if (node == null)
                continue;
            node.LoadChildren();
            nodes.Add(node);
        }
        return nodes;
    }

    private BrowseNode?[] FindNodes(List<NodePath> places, CancellationToken cancellationToken)
    {
        var found = new BrowseNode?[places.Count];
        for (var i = 0; i < found.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            found[i] = Assemblies.FindNode(places[i]);
        }
        return found;
    }

    private void OnReloaded(Reloaded? reloaded, Exception? error, ReloadRequest request)
    {
        if (!Reload.Finish(request.Ticket))
            return;
        if (error != null)
        {
            ReportError(GetStrings().ErrorReloadAssemblies, error.ToString());
            return;
        }
        ReportAll(reloaded!.Errors);
        Tree.Expand(reloaded.Expanded);
        for (var i = 0; i < request.Tabs.Count; i++)
            ReshowTab(request.Tabs[i], reloaded.Places[i], request.Intent);
        SaveSettings();
    }

    private void ReshowTab(ReloadingTab reloading, BrowseNode? node, int intent)
    {
        var tab = reloading.Tab;
        if (!Tabs.Contains(tab))
            return;
        if (tab.Version != reloading.Version)
            return;
        if (node == null)
        {
            ReleaseGoneTab(tab, reloading.Location);
            return;
        }
        tab.Show(node, Language, reloading.Position);
        if (tab == Tabs.Active && Intent.IsCurrent(intent))
            Tree.Reveal(node);
    }

    private void ReleaseGoneTab(DocumentTab tab, NodePath location)
    {
        var t = GetStrings();
        tab.Release(t.DocumentGone(location.Text));
        ReportError(t.ErrorFindAfterReload(location.Text), location.ToString());
    }

    public void SetLocale(Locale locale)
    {
        if (locale == Locale)
            return;
        Locale = locale;
        SaveSettings();
    }

    public void SetLanguage(DecompilationLanguage language)
    {
        if (language == Language)
            return;
        Language = language;
        EnsureCurrent();
    }

    public void SetLanguageVersion(CSharpVersion version)
    {
        var options = DecompilerOptions with { LanguageVersion = version };
        SetDecompilerOptions(options);
    }

    public void SetDecompilerToggle(DecompilerToggle toggle, bool value)
    {
        var options = DecompilerOptions.With(toggle, value);
        SetDecompilerOptions(options);
    }

    public void SetDecompilerToggles(IEnumerable<DecompilerToggle> toggles, bool value)
    {
        var options = DecompilerOptions.With(toggles, value);
        SetDecompilerOptions(options);
    }

    /// <summary>Turns every toggle back to its default, keeping the language version.</summary>
    public void ResetDecompilerToggles()
    {
        var options = DecompilerOptions with { Toggles = DecompilerOptions.Default.Toggles };
        SetDecompilerOptions(options);
    }

    /// <summary>Makes <paramref name="options"/> current and decompiles the shown C# documents again with them.</summary>
    private void SetDecompilerOptions(DecompilerOptions options)
    {
        if (options == DecompilerOptions)
            return;
        DecompilerOptions = options;
        EnsureCurrent();
    }

    private void EnsureCurrent()
    {
        foreach (var group in Layout.AllGroups())
            Tabs.Find(group.Active)?.EnsureCurrent(Language);
    }

    /// <summary>Writes the bytes of the resource <paramref name="node"/> stands for to the file at
    /// <paramref name="path"/>.</summary>
    public void SaveResource(BrowseNode node, string path)
    {
        try
        {
            var bytes = node.ReadResource();
            File.WriteAllBytes(path, bytes);
        }
        catch (Exception e) when (IsFileError(e))
        {
            ReportError(GetStrings().ErrorSaveResource(node.Text), e.Message);
        }
    }

    private static bool IsFileError(Exception e)
    {
        if (e is IOException)
            return true;
        if (e is UnauthorizedAccessException)
            return true;
        return e is InvalidDataException;
    }

    /// <summary>Analyzes the type or member <paramref name="node"/> stands for in the analyzer, which is
    /// opened if it is not.</summary>
    public void Analyze(BrowseNode node)
    {
        var symbol = Analysis.Of(node);
        if (symbol == null)
            return;
        OpenAnalyzer();
        Analyzer.Analyze(symbol);
    }

    /// <summary>Finds what <paramref name="reference"/> points at in the background, opening its
    /// assembly if it is not, and analyzes it.</summary>
    public void Analyze(CodeReference reference)
    {
        RunInBackground(() => Assemblies.FindNode(reference), (node, error) => OnAnalyzeTargetFound(reference, node, error));
    }

    private void OnAnalyzeTargetFound(CodeReference reference, BrowseNode? node, Exception? error)
    {
        if (node == null)
        {
            ReportUnresolved(reference, error);
            return;
        }
        Analyze(node);
    }

    internal void ReportUnresolved(CodeReference reference, Exception? error)
    {
        var t = GetStrings();
        var details = DescribeFailure(error, t.ErrorNoDefinition(reference));
        ReportError(t.ErrorResolve(reference.Name), details);
    }

    public void OpenAnalyzer() => _mainLayout.OpenAnalyzer(Tabs.ActivePanel);

    public void OpenDecompilerOptions() => _mainLayout.OpenDecompilerOptions(Tabs.ActivePanel);

    public void OpenBookmarks() => _mainLayout.OpenBookmarks(Tabs.ActivePanel);

    public void Activate(DocumentTab tab)
    {
        if (!Tabs.Contains(tab))
            return;
        Layout.Activate(tab.Panel);
        Focus(tab);
    }

    /// <summary>Called after the documents are recorded: makes the tab the user last pressed, docked
    /// or brought to front the active one.</summary>
    public void SyncActiveTab()
    {
        var tab = Tabs.Find(Layout.Focused);
        if (tab == null)
            return;
        if (tab == Tabs.Active)
            return;
        Focus(tab);
    }

    /// <summary>Brings the next tab of the active tab's group to the front, or the previous one.</summary>
    public void CycleTab(int by)
    {
        var active = Tabs.Active;
        if (active == null)
            return;
        var group = Layout.GroupOf(active.Panel);
        if (group == null)
            return;
        var panels = group.Panels.FindAll(IsTabPanel);
        if (panels.Count < 2)
            return;
        var index = panels.IndexOf(active.Panel);
        var next = panels[WrapIndex(index + by, panels.Count)];
        var tab = Tabs.Find(next);
        if (tab == null)
            return;
        Activate(tab);
    }

    private static int WrapIndex(int index, int count)
    {
        var wrapped = index % count;
        if (wrapped < 0)
            wrapped += count;
        return wrapped;
    }

    public void CloseTab(DocumentTab tab) => Layout.Close(tab.Panel);

    /// <summary>Asks the user whether to reset the layout before <see cref="ResetLayout"/> does.</summary>
    public void AskToResetLayout() => ConfirmingLayoutReset = true;

    /// <summary>Stops asking, resetting the layout when <paramref name="confirmed"/>.</summary>
    public void FinishLayoutReset(bool confirmed)
    {
        ConfirmingLayoutReset = false;
        if (confirmed)
            ResetLayout();
    }

    /// <summary>Puts the layout back as it starts out: the assemblies left of every open document,
    /// tabbed together in one group. Floating windows and the other views close; the documents stay open.</summary>
    public void ResetLayout()
    {
        var documents = _mainLayout.ListDocuments();
        if (documents.Count == 0)
            documents.Add(AddActiveTab().Panel);
        _mainLayout.Reset(documents);
        ActivateActivePanel();
    }

    private DocumentTab AddActiveTab()
    {
        var tab = AddTab();
        Tabs.MakeActive(tab);
        return tab;
    }

    private void ActivateActivePanel()
    {
        var panel = Tabs.ActivePanel;
        if (panel == null)
            return;
        if (!Layout.Contains(panel))
            return;
        Layout.Activate(panel);
    }

    public void OpenAssemblies() => _mainLayout.OpenAssemblies();

    public void SetMenuTarget(BrowseNode node) => MenuTarget = node;

    /// <summary>Docks the search panel under the documents if it is closed, brings it to the front
    /// and puts the caret in its search box.</summary>
    public void OpenSearch()
    {
        _mainLayout.OpenSearch(Tabs.ActivePanel);
        _focusSearch = true;
    }

    /// <summary>Opens the search panel with its query kept to <paramref name="node"/>'s assembly, or to
    /// the namespace it is.</summary>
    public void ScopeSearch(BrowseNode node)
    {
        Search.Query = ScopeQueryTo(node);
        OpenSearch();
    }

    private string ScopeQueryTo(BrowseNode node)
    {
        if (node.Kind == NodeKind.Namespace)
            return SearchQuery.WithScope(Search.Query, NamespaceScope, node.FullName);
        return SearchQuery.WithScope(Search.Query, AssemblyScope, node.Assembly.Name);
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
        CopyStateToSettings();
        LastSave.Record(Bookmarks, Analyzer, MetadataFilters);
        WriteSettingsFile();
    }

    private void CopyStateToSettings()
    {
        _settings.Assemblies = Assemblies.FilePaths.ToList();
        _settings.Theme = Theme;
        _settings.Language = Language;
        _settings.Decompiler = DecompilerOptions;
        _settings.FoldedOptionGroups = FoldedOptionGroups
            .Order()
            .ToList();
        _settings.ShowNonPublic = ShowNonPublic;
        _settings.SearchKinds = Search.Kinds;
        _settings.Locale = Locale;
        _settings.Layout = Layout;
        _settings.Bookmarks = Bookmarks.Save();
        if (!_isRestoringAnalyzed)
            _settings.Analyzed = ListAnalyzedToSave();
        _settings.BookmarkColumns = BookmarkColumns.Weights.ToList();
        _settings.SearchColumns = SearchColumns.Weights.ToList();
        _settings.FlagsFilters = MetadataFilters.Save();
    }

    private List<CodeAnchor> ListAnalyzedToSave()
    {
        if (_analyzedToRestore != null)
            return _analyzedToRestore;
        return Analyzer.Roots
            .Select(r => r.Symbol.Definition)
            .OfType<CodeAnchor>()
            .ToList();
    }

    private void WriteSettingsFile()
    {
        var error = _settings.Save();
        if (error == null)
            return;
        ReportError(GetStrings().ErrorSaveSettings, error);
    }

    internal Strings GetStrings() => Strings.For(Locale);

    internal bool IsListed(BrowseNode node) => Assemblies.Assemblies.Contains(node.Assembly);

    private bool IsTabPanel(string panel) => Tabs.Find(panel) != null;

    private DocumentTab AddTab()
    {
        var tab = new DocumentTab(Post, GetStrings, () => DecompilerOptions, ReportError);
        Tabs.Add(tab);
        return tab;
    }

    private void RemoveTab(string panel)
    {
        var tab = Tabs.Find(panel);
        if (tab == null)
            return;
        Intent.Begin();
        var wasActive = tab == Tabs.Active;
        Tabs.Remove(tab);
        if (!wasActive)
            return;
        FocusNextTab();
    }

    private void FocusNextTab()
    {
        var next = Tabs.Find(Layout.Focused);
        if (next == null)
            next = FindFrontTab();
        if (next == null)
            return;
        Focus(next);
    }

    private DocumentTab? FindFrontTab()
    {
        foreach (var group in Layout.AllGroups())
        {
            var tab = Tabs.Find(group.Active);
            if (tab != null)
                return tab;
        }
        return null;
    }

    private void Focus(DocumentTab tab)
    {
        Intent.Begin();
        Tabs.MakeActive(tab);
        tab.EnsureCurrent(Language);
        var node = tab.Node;
        if (node != null && IsListed(node))
            Tree.Reveal(node);
    }

    internal void LeaveActive()
    {
        var tab = Tabs.Active;
        if (tab == null)
            return;
        var position = tab.Position;
        if (position == null)
            return;
        if (!IsCurrentHistoryPlace(tab.Location))
            return;
        History.Leave(tab.Language, position.Value);
    }

    private bool IsCurrentHistoryPlace(NodePath? place)
    {
        var current = History.Current;
        if (current == null)
            return false;
        return current.Value.Place.Equals(place);
    }

    internal void ShowIn(
        DocumentTab? tab,
        BrowseNode node,
        CodeViewPosition? position = null,
        Bookmark? target = null,
        int row = MetadataGridState.None)
    {
        var shownIn = ActivateOrOpenTab(tab);
        Tabs.MakeActive(shownIn);
        shownIn.Show(node, Language, position, target, row);
        Tree.Reveal(node);
    }

    private DocumentTab ActivateOrOpenTab(DocumentTab? tab)
    {
        if (tab == null)
            return OpenNewTab();
        Layout.Activate(tab.Panel);
        return tab;
    }

    private DocumentTab OpenNewTab()
    {
        var tab = AddTab();
        _mainLayout.DockDocument(tab.Panel, Tabs.ActivePanel);
        return tab;
    }

    private void ReportAll(List<(string Title, string Details)> errors)
    {
        foreach (var (title, details) in errors)
            ReportError(title, details);
    }

    internal static string DescribeFailure(Exception? error, string fallback)
    {
        if (error == null)
            return fallback;
        return error.ToString();
    }

    internal void RunInBackground<T>(Func<T> work, Action<T?, Exception?> done)
    {
        var task = Task.Run(work);
        task.ContinueWith(finished => PostCompletion(finished, done), TaskScheduler.Default);
    }

    private void PostCompletion<T>(Task<T> task, Action<T?, Exception?> done)
    {
        Post(() => Complete(task, done));
    }

    private static void Complete<T>(Task<T> task, Action<T?, Exception?> done)
    {
        if (task.IsCompletedSuccessfully)
        {
            done(task.Result, null);
            return;
        }
        if (task.IsCanceled)
            return;
        var error = task.Exception!.GetBaseException();
        done(default, error);
    }

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
                errors.Add(DescribeLoadFailure(path, e));
            }
        }
        return new Opened(last, errors);
    }

    private (string Title, string Details) DescribeLoadFailure(string path, AssemblyLoadException e)
    {
        var file = Path.GetFileName(path);
        var title = GetStrings().ErrorOpenFile(file);
        var details = DescribeFailure(e.InnerException, e.Message);
        return (title, details);
    }

    private sealed record Opened(LoadedAssembly? Last, List<(string Title, string Details)> Errors);

    private sealed record Reloaded(List<(string Title, string Details)> Errors, List<BrowseNode> Expanded, BrowseNode?[] Places);

    private sealed record ReloadingTab(DocumentTab Tab, NodePath Location, int Version, CodeViewPosition? Position);

    private sealed record ReloadRequest(ReloadTicket Ticket, int Intent, List<ReloadingTab> Tabs);
}
