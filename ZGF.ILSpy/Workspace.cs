using ZGF.Gui.Immediate.Widgets;
using System.Globalization;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>
/// The app's state. What the user can do to it are extension methods grouped by feature, such as
/// <see cref="Navigation"/> and <see cref="Bookmarking"/>. Lives on the UI thread: background work
/// (opening, decompiling, resolving, searching) is awaited there through <see cref="Background"/>.
/// A result is dropped when the user asked for something else in the meantime.
/// </summary>
public sealed class Workspace
{
    public AppSettings Settings { get; }
    public AssemblyList Assemblies { get; } = new();

    /// <summary>The named assembly lists, one of which <see cref="Assemblies"/> holds open.</summary>
    public SavedAssemblyLists Lists { get; } = new();

    /// <summary>What the assembly lists dialog shows, and whether it is open.</summary>
    public AssemblyListsDialog ListsDialog { get; } = new();
    public BrowseTree Tree { get; }
    public NavigationHistory History { get; } = new();
    public UserIntent Intent { get; } = new();
    public Reload Reload { get; } = new();
    public OpenRequests Opens { get; } = new();
    public LastSave LastSave { get; } = new();
    public PendingAnalyses PendingAnalyses { get; }
    public BookmarkList Bookmarks { get; } = new();

    /// <summary>The symbols being analyzed and what the analyses found.</summary>
    public AnalyzerTree Analyzer { get; }

    /// <summary>The columns of the bookmarks panel: mark, name, location and module.</summary>
    public TableColumns BookmarkColumns { get; } = new(
        new TableColumn(Fixed: 28f, Controls: true),
        new TableColumn(2f, 80f),
        new TableColumn(3f, 80f),
        new TableColumn(2f, 60f));

    /// <summary>The columns of the export dialog's table: assembly, version and project name.</summary>
    public TableColumns ExportColumns { get; } = new(
        new TableColumn(3f, 120f),
        new TableColumn(1f, 64f),
        new TableColumn(3f, 120f));

    /// <summary>The columns of the search results: name, location and assembly.</summary>
    public TableColumns SearchColumns { get; } = new(
        new TableColumn(3f, 120f),
        new TableColumn(3f, 80f),
        new TableColumn(2f, 60f));

    public Search Search { get; }

    /// <summary>The metadata grids' flags filters.</summary>
    public MetadataFilters MetadataFilters { get; }
    public FrameworkPicker Picker { get; }
    public ErrorLog Errors { get; } = new();
    public DocumentTabs Tabs { get; } = new();

    /// <summary>The declarations and documentation shown when hovering types and members in code.</summary>
    public CodeTooltips Tooltips { get; }

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

    /// <summary>True while the open list's analyses are being found to be analyzed again.</summary>
    public bool IsRestoringAnalyses => PendingAnalyses.Pending != null;

    /// <summary>Bookmarks read from a file, waiting for the user to say whether they replace the
    /// ones there are or merge in beside them.</summary>
    public List<Bookmark>? PendingImport { get; set; }

    /// <summary>True while imported bookmarks' places are being found in the background.</summary>
    public bool IsImporting { get; set; }

    /// <summary>The code, projects, solutions and PDBs being written in the background, one at a time.</summary>
    public ExportQueue Export { get; } = new();

    /// <summary>What the export dialog asks for, and whether it is open.</summary>
    public ExportSetup ExportSetup { get; } = new();

    /// <summary>The reports of the exports that finished, oldest first, until they are taken off the
    /// exports panel.</summary>
    public List<ExportReport> FinishedExports { get; } = new();

    /// <summary>The run of the export selected in the exports panel, or 0; one that is gone is as none.</summary>
    public int SelectedExport;

    /// <summary>The run of the export the exports panel's context menu was opened on, or 0.</summary>
    public int ExportMenuTarget;

    /// <summary>True while <see cref="Export"/> runs.</summary>
    public bool IsExporting => Export.IsRunning;


    /// <summary>Set by <see cref="Searching.OpenSearch"/> until the search box takes focus.</summary>
    public bool IsSearchFocusPending { get; set; }

    /// <summary>Set while the user is asked whether to reset the layout.</summary>
    public bool ConfirmingLayoutReset { get; set; }

    public Workspace(AppSettings settings)
    {
        Settings = settings;
        Tooltips = new CodeTooltips(Assemblies);
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
        Tree = new BrowseTree();
        Analyzer = new AnalyzerTree(Assemblies);
        Search = new Search(() => Strings.For(Locale), ReportError);
        Search.Kinds = settings.SearchKinds;
        MetadataFilters = new MetadataFilters(settings.FlagsFilters);
        Lists.Load(settings.AssemblyLists, settings.ActiveAssemblyList);
        Bookmarks.Load(settings.Bookmarks);
        BookmarkColumns.Restore(settings.BookmarkColumns);
        SearchColumns.Restore(settings.SearchColumns);
        ExportColumns.Restore(settings.ExportColumns);
        ExportSetup.Window = settings.ExportWindow;
        LastSave.Record(Assemblies, Lists, Bookmarks, Analyzer, MetadataFilters);
        Picker = new FrameworkPicker(() => Strings.For(Locale), ReportError);
        if (!placed)
            MainLayout.DockDocument(first.Panel, first.Panel);
        PendingAnalyses = new PendingAnalyses(Lists.ActiveAnalyses);
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
        ReportError(Strings.For(Locale).ErrorReadSettings, settings.LoadError);
    }

    public void ReportError(string title, string? details = null) => Errors.Add(title, details);

    internal bool IsListed(BrowseNode node) => Assemblies.Assemblies.Contains(node.Assembly);

    /// <summary>True when the assembly at <paramref name="filePath"/> is open in the active list.</summary>
    internal bool IsListed(string filePath)
    {
        var listed = Assemblies.Assemblies;
        for (var i = 0; i < listed.Count; i++)
        {
            if (string.Equals(listed[i].FilePath, filePath, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    internal DocumentTab AddTab()
    {
        var tab = new DocumentTab(() => Strings.For(Locale), () => DecompilerOptions, ReportError);
        Tabs.Add(tab);
        return tab;
    }

    internal static string DescribeFailure(Exception? error, string fallback)
    {
        if (error == null)
            return fallback;
        return error.ToString();
    }
}
