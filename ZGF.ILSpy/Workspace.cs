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
    private readonly AppSettings _settings;
    private Action<Action> _post = ThrowNotAttached;

    public AssemblyList Assemblies { get; } = new();
    public BrowseTree Tree { get; }
    public NavigationHistory History { get; } = new();
    public UserIntent Intent { get; } = new();
    public Reload Reload { get; } = new();
    public LastSave LastSave { get; } = new();
    public LastSessionAnalyses LastSessionAnalyses { get; }
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
    public MainLayout MainLayout { get; }

    public DockLayout Layout => MainLayout.Dock;

    public DecompilationLanguage Language { get; set; }

    /// <summary>How C# is decompiled.</summary>
    public DecompilerOptions DecompilerOptions { get; set; }

    /// <summary>The decompiler options panel's folded groups.</summary>
    public HashSet<string> FoldedOptionGroups { get; }

    /// <summary>The node the tree's context menu was opened on.</summary>
    public BrowseNode? MenuTarget { get; set; }

    /// <summary>True while a reference or a history entry is being found in the background.</summary>
    public bool IsResolving => Intent.IsResolving;

    /// <summary>True while <see cref="AssemblyLoading.Refresh"/> reopens the assemblies.</summary>
    public bool IsReloading => Reload.IsRunning;

    /// <summary>Bookmarks read from a file, waiting for the user to say whether they replace the
    /// ones there are or merge in beside them.</summary>
    public List<Bookmark>? PendingImport { get; set; }

    /// <summary>True while imported bookmarks' places are being found in the background.</summary>
    public bool IsImporting { get; set; }

    /// <summary>Set by <see cref="Searching.OpenSearch"/> until the search box takes focus.</summary>
    public bool IsSearchFocusPending { get; set; }

    /// <summary>Set while the user is asked whether to reset the layout.</summary>
    public bool ConfirmingLayoutReset { get; set; }

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
        MainLayout = new MainLayout(dock);
        Layout.NewPanel = () => AddTab().Panel;
        Layout.PanelClosed += this.OnPanelClosed;
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
            MainLayout.DockDocument(first.Panel, first.Panel);
        LastSessionAnalyses = new LastSessionAnalyses(settings.Analyzed);
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
        this.RestoreLastSessionAnalyses();
        MetadataFilters.Sync();
        var treeChanged = Tree.Sync(Assemblies);
        if (IsReloading)
            return;
        if (!treeChanged && !LastSave.IsBehind(Bookmarks, Analyzer, MetadataFilters))
            return;
        SaveSettings();
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

    internal void ReportUnresolved(CodeReference reference, Exception? error)
    {
        var t = GetStrings();
        var details = DescribeFailure(error, t.ErrorNoDefinition(reference));
        ReportError(t.ErrorResolve(reference.Name), details);
    }

    public void SetMenuTarget(BrowseNode node) => MenuTarget = node;

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
        _settings.Analyzed = LastSessionAnalyses.ListToSave(Analyzer);
        _settings.BookmarkColumns = BookmarkColumns.Weights.ToList();
        _settings.SearchColumns = SearchColumns.Weights.ToList();
        _settings.FlagsFilters = MetadataFilters.Save();
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

    internal DocumentTab AddTab()
    {
        var tab = new DocumentTab(Post, GetStrings, () => DecompilerOptions, ReportError);
        Tabs.Add(tab);
        return tab;
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
        MainLayout.DockDocument(tab.Panel, Tabs.ActivePanel);
        return tab;
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
}
