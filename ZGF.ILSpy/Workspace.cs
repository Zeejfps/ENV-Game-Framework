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
    private readonly UserIntent _intent = new();
    private readonly MainLayout _mainLayout;
    private int _reload;
    private int _savedBookmarks;
    private int _savedAnalyzer;
    private int _savedFilters;
    private List<CodeAnchor>? _analyzedToRestore;
    private bool _isRestoringAnalyzed;
    private CancellationTokenSource? _reloadWork;
    private bool _focusSearch;

    public AssemblyList Assemblies { get; } = new();
    public BrowseTree Tree { get; }
    public NavigationHistory History { get; } = new();
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
    public bool IsResolving => _intent.IsResolving;

    /// <summary>True while <see cref="Refresh"/> reopens the assemblies.</summary>
    public bool IsReloading => _reloadWork != null;

    /// <summary>Bookmarks read from a file, waiting for the user to say whether they replace the
    /// ones there are or merge in beside them.</summary>
    public List<Bookmark>? PendingImport { get; private set; }

    /// <summary>True while imported bookmarks' places are being found in the background.</summary>
    public bool IsImporting { get; private set; }

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
        _savedBookmarks = Bookmarks.Revision;
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
        if (!treeChanged && !HasUnsavedRevisions())
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
        _savedAnalyzer = Analyzer.Revision;
    }

    private void AnalyzeAll(List<AnalysisSymbol?> symbols)
    {
        foreach (var symbol in symbols)
        {
            if (symbol != null)
                Analyzer.Analyze(symbol);
        }
    }

    private bool HasUnsavedRevisions()
    {
        if (Bookmarks.Revision != _savedBookmarks)
            return true;
        if (Analyzer.Revision != _savedAnalyzer)
            return true;
        return MetadataFilters.Revision != _savedFilters;
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
        var ticket = _intent.Begin();
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
        if (_intent.IsStale(ticket))
            return;
        Show(opened.Last.Root);
    }

    public void Open(string path) => Open([path]);

    public void Close(LoadedAssembly assembly) => Assemblies.Close(assembly);

    public void ClearList()
    {
        _intent.Begin();
        StopReload();
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
        var intent = _intent.Begin();
        StopReload();
        _reload++;
        var work = new CancellationTokenSource();
        _reloadWork = work;

        var paths = Assemblies.FilePaths;
        var expanded = Tree.ExpandedPaths();
        var tabs = ReleaseShownTabs();
        var places = tabs.ConvertAll(t => t.Location);
        ClearAssemblies();

        var request = new ReloadRequest(work, _reload, intent, tabs);
        var token = work.Token;
        RunInBackground(() => Reload(paths, expanded, places, token), (reloaded, error) => OnReloaded(reloaded, error, request));
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

    private Reloaded Reload(IReadOnlyList<string> paths, List<NodePath> expanded, List<NodePath> places, CancellationToken cancellationToken)
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
        request.Work.Dispose();
        if (request.Reload != _reload)
            return;
        _reloadWork = null;
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
        if (tab == Tabs.Active && _intent.IsCurrent(intent))
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

    /// <summary>Shows <paramref name="node"/> in the active tab, or a new one, reveals it in the tree
    /// and records it in the history.</summary>
    public void Show(BrowseNode node, bool inNewTab = false)
    {
        if (node.Kind == NodeKind.Loading)
            return;
        _intent.Begin();
        LeaveActive();
        var tab = inNewTab ? null : Tabs.Active;
        ShowIn(tab, node);
        History.Push(node, Language);
    }

    /// <summary>Resolves <paramref name="reference"/> in the background (it may load an assembly),
    /// then shows it in the tab it was asked from, or a new one.</summary>
    public void Navigate(CodeReference reference, bool inNewTab)
    {
        if (!reference.IsNavigable)
            return;
        var ticket = _intent.Begin();
        var origin = inNewTab ? null : Tabs.Active;
        _intent.StartResolving(ticket);
        RunInBackground(() => Assemblies.FindNode(reference), (node, error) => OnReferenceFound(reference, node, error, origin, ticket));
    }

    private void OnReferenceFound(CodeReference reference, BrowseNode? node, Exception? error, DocumentTab? origin, int ticket)
    {
        if (_intent.IsStale(ticket))
            return;
        _intent.StopResolving();
        if (node == null)
        {
            ReportUnresolved(reference, error);
            return;
        }
        LeaveActive();
        ShowIn(Tabs.KeepIfOpen(origin), node);
        History.Push(node, Language);
    }

    private void ReportUnresolved(CodeReference reference, Exception? error)
    {
        var t = GetStrings();
        var details = DescribeFailure(error, t.ErrorNoDefinition(reference));
        ReportError(t.ErrorResolve(reference.Name), details);
    }

    /// <summary>Shows the table row a metadata token leads to in the active tab, or a new one, and
    /// records it in the history.</summary>
    public void Show(MetadataTarget target, bool inNewTab)
    {
        _intent.Begin();
        LeaveActive();
        var tab = inNewTab ? null : Tabs.Active;
        ShowIn(tab, target.Node, row: target.Row);
        History.Push(target.Node, Language);
    }

    /// <summary>Goes where a node points (a base type, an assembly reference), if anywhere.</summary>
    public void GoToTarget(BrowseNode node)
    {
        if (node.Target == null)
            return;
        Navigate(node.Target, inNewTab: false);
    }

    public void GoBack() => GoTo(History.CurrentIndex - 1);

    public void GoForward() => GoTo(History.CurrentIndex + 1);

    /// <summary>Goes to the history entry at <paramref name="index"/>, however far back or forward.</summary>
    public void GoTo(int index)
    {
        if (!CanGoTo(index))
            return;
        LeaveActive();
        var entry = History.MoveTo(index);
        if (entry == null)
            return;
        Restore(entry.Value);
    }

    private bool CanGoTo(int index)
    {
        if (index < 0)
            return false;
        if (index >= History.Entries.Count)
            return false;
        return index != History.CurrentIndex;
    }

    private void Restore(HistoryEntry entry)
    {
        var ticket = _intent.Begin();
        var tab = Tabs.Active;
        var place = entry.Place;
        var position = entry.Language == Language ? entry.Position : null;
        var shown = FindShownNode(tab, place);
        if (shown != null)
        {
            ShowIn(tab, shown, position);
            return;
        }
        _intent.StartResolving(ticket);
        RunInBackground(() => Assemblies.FindNode(place), (node, error) => OnHistoryPlaceFound(place, position, node, error, tab, ticket));
    }

    private BrowseNode? FindShownNode(DocumentTab? tab, NodePath place)
    {
        if (tab == null)
            return null;
        var node = tab.Node;
        if (node == null)
            return null;
        if (!place.Equals(tab.Location))
            return null;
        if (!IsListed(node))
            return null;
        return node;
    }

    private void OnHistoryPlaceFound(NodePath place, CodeViewPosition? position, BrowseNode? node, Exception? error, DocumentTab? tab, int ticket)
    {
        if (_intent.IsStale(ticket))
            return;
        _intent.StopResolving();
        if (node == null)
        {
            var t = GetStrings();
            var details = DescribeFailure(error, t.ErrorNoLongerThere(place));
            ReportError(t.ErrorGoBack(place.Text), details);
            return;
        }
        ShowIn(Tabs.KeepIfOpen(tab), node, position);
    }

    /// <summary>Removes the bookmark marking <paramref name="line"/> of <paramref name="tab"/>'s
    /// document, or bookmarks the line, naming the bookmark by the statement or definition on it
    /// when it has one.</summary>
    public void ToggleBookmark(DocumentTab tab, int line)
    {
        var node = tab.Node;
        if (node == null)
            return;
        if (!CanBookmarkLine(tab, line))
            return;
        var existing = Bookmarks.At(tab.Document, node.Path, line);
        if (existing != null)
        {
            Bookmarks.Remove(existing);
            return;
        }
        AddBookmark(tab, node, line);
    }

    private static bool CanBookmarkLine(DocumentTab tab, int line)
    {
        if (tab.IsBusy)
            return false;
        if (tab.Document.IsError)
            return false;
        if (line < 0)
            return false;
        return line < tab.Document.Lines.Length;
    }

    private void AddBookmark(DocumentTab tab, BrowseNode node, int line)
    {
        var anchor = tab.Document.AnchorAt(line);
        var name = Bookmarks.NextName(FormatBookmarkName);
        var info = ILSpyTree.Describe(node, anchor);
        var bookmark = new Bookmark(node.Path, tab.Language, line, anchor, name, info);
        Bookmarks.Add(bookmark);
    }

    private string FormatBookmarkName(int number) => GetStrings().BookmarksDefaultName(number);

    /// <summary>Shows the line <paramref name="bookmark"/> marks in the active tab, opening its
    /// assembly first if it is not open, and records it in the history.</summary>
    public void GoTo(Bookmark bookmark)
    {
        var ticket = _intent.Begin();
        var tab = Tabs.Active;
        var place = bookmark.Place;
        _intent.StartResolving(ticket);
        RunInBackground(() => Assemblies.FindNode(place), (node, error) => OnBookmarkPlaceFound(bookmark, node, error, tab, ticket));
    }

    private void OnBookmarkPlaceFound(Bookmark bookmark, BrowseNode? node, Exception? error, DocumentTab? tab, int ticket)
    {
        if (_intent.IsStale(ticket))
            return;
        _intent.StopResolving();
        if (node == null)
        {
            var t = GetStrings();
            var details = DescribeFailure(error, t.ErrorNoLongerThere(bookmark.Place));
            ReportError(t.ErrorGoToBookmark(bookmark.Name), details);
            return;
        }
        LeaveActive();
        ShowIn(Tabs.KeepIfOpen(tab), node, target: bookmark);
        History.Push(node, Language);
    }

    /// <summary>Goes to the next enabled bookmark after the selected one, or the previous when
    /// <paramref name="direction"/> is negative.</summary>
    public void StepBookmark(int direction)
    {
        var bookmark = Bookmarks.Step(direction);
        if (bookmark == null)
            return;
        GoTo(bookmark);
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

    public void ExportBookmarks(string path)
    {
        var records = Bookmarks.Save();
        var error = BookmarkFile.Write(path, records);
        if (error == null)
            return;
        ReportError(GetStrings().ErrorExportBookmarks, error);
    }

    /// <summary>Reads the bookmarks in the file at <paramref name="path"/> — this app's or the real
    /// ILSpy's — and finds the place of each in the background: takes them at once when there are
    /// none yet, and otherwise asks how through <see cref="PendingImport"/>. Bookmarks whose place
    /// is not found are left out and reported.</summary>
    public void ImportBookmarks(string path)
    {
        var file = Path.GetFileName(path);
        var records = BookmarkFile.Read(path, out var error);
        if (records == null)
        {
            ReportError(GetStrings().ErrorImportBookmarks(file), error);
            return;
        }
        IsImporting = true;
        RunInBackground(() => FindPlaces(records), (found, failure) => OnBookmarkPlacesFound(found, failure, file));
    }

    private List<FoundBookmark> FindPlaces(List<BookmarkRecord> records)
    {
        return records.ConvertAll(r => new FoundBookmark(r, FindPlace(r)));
    }

    private NodePath? FindPlace(BookmarkRecord record)
    {
        if (record.Place != null)
            return NodePath.From(record.FileName, record.Place);
        var treePath = record.ViewState?.SelectedTreeNodePath;
        return ILSpyTree.Locate(Assemblies, record.FileName, treePath, record.Token);
    }

    private void OnBookmarkPlacesFound(List<FoundBookmark>? found, Exception? failure, string file)
    {
        IsImporting = false;
        if (failure != null)
        {
            ReportError(GetStrings().ErrorImportBookmarks(file), failure.ToString());
            return;
        }
        ReportLostBookmarks(found!, file);
        var bookmarks = found!
            .Where(f => f.Place != null)
            .Select(f => Bookmark.FromRecord(f.Record, f.Place!))
            .ToList();
        if (Bookmarks.Items.Count == 0)
        {
            Bookmarks.Import(bookmarks, BookmarkImportMode.Replace);
            return;
        }
        if (bookmarks.Count == 0)
            return;
        PendingImport = bookmarks;
    }

    private void ReportLostBookmarks(List<FoundBookmark> found, string file)
    {
        var lost = found.FindAll(f => f.Place == null);
        if (lost.Count == 0)
            return;
        var lines = lost.Select(DescribeLostBookmark);
        var details = string.Join(Environment.NewLine, lines);
        ReportError(GetStrings().ErrorBookmarksNotFound(file), details);
    }

    private static string DescribeLostBookmark(FoundBookmark lost)
    {
        var record = lost.Record;
        var location = record.LocationNodeName ?? record.MemberName;
        return $"{record.Name}: {location} ({record.FileName})";
    }

    /// <summary>Imports the <see cref="PendingImport"/> as <paramref name="mode"/> says, or drops it when null.</summary>
    public void FinishImport(BookmarkImportMode? mode)
    {
        if (mode != null && PendingImport != null)
            Bookmarks.Import(PendingImport, mode.Value);
        PendingImport = null;
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
        MarkRevisionsSaved();
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

    private void MarkRevisionsSaved()
    {
        _savedAnalyzer = Analyzer.Revision;
        _savedFilters = MetadataFilters.Revision;
        _savedBookmarks = Bookmarks.Revision;
    }

    private void WriteSettingsFile()
    {
        var error = _settings.Save();
        if (error == null)
            return;
        ReportError(GetStrings().ErrorSaveSettings, error);
    }

    private Strings GetStrings() => Strings.For(Locale);

    private bool IsListed(BrowseNode node) => Assemblies.Assemblies.Contains(node.Assembly);

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
        _intent.Begin();
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
        _intent.Begin();
        Tabs.MakeActive(tab);
        tab.EnsureCurrent(Language);
        var node = tab.Node;
        if (node != null && IsListed(node))
            Tree.Reveal(node);
    }

    private void LeaveActive()
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

    private void ShowIn(
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

    private static string DescribeFailure(Exception? error, string fallback)
    {
        if (error == null)
            return fallback;
        return error.ToString();
    }

    private void RunInBackground<T>(Func<T> work, Action<T?, Exception?> done)
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

    private sealed record ReloadRequest(CancellationTokenSource Work, int Reload, int Intent, List<ReloadingTab> Tabs);

    private sealed record FoundBookmark(BookmarkRecord Record, NodePath? Place);
}
