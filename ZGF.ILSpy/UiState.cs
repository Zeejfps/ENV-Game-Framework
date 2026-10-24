using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.ScreenLayout;
using static ZGF.ILSpy.ScreenSettings;

namespace ZGF.ILSpy;

/// <summary>The screen, one per app: what every view is drawn with this frame (the theme, the editor's
/// and icons' colors, the strings of the chosen language), and the state that exists only because there
/// is a screen: the layout, each tab's view, the trees' and tables' state, the panels and dialogs with
/// what is selected in them, and UI flags. What the user does to it are static functions grouped by
/// feature, such as <see cref="DocumentPanels"/> and <see cref="ScreenLayout"/>. It and the app state never
/// see each other: the app hands it what it shows each frame and runs what it asks of the app state
/// through <see cref="AppCommands"/>.</summary>
public sealed class UiState : IUiState
{
    /// <param name="saved">What the settings kept of the screen when they were last saved.</param>
    /// <param name="reportError">Tells the user that something done in the background failed: a title and details.</param>
    public UiState(ViewSettings saved, Action<string, string> reportError)
    {
        FoldedOptionGroups = new FoldedGroups(saved.FoldedOptionGroups);
        BookmarkColumns.Restore(saved.BookmarkColumns);
        SearchColumns.Restore(saved.SearchColumns);
        ExportColumns.Restore(saved.ExportColumns);
        Search = new Search(ReadStrings, reportError);
        Search.Kinds = saved.SearchKinds;
        MetadataFilters = new MetadataFilters(saved.FlagsFilters);
        Picker = new FrameworkPicker(ReadStrings, reportError);
        ExportSetup.RestoreWindow(saved.ExportWindow);
        Display = DisplayOptions.Restore(saved.Display);
        Analyzer = new AnalyzerRows(AppCommands);
        RestoredDocumentsPanel = Documents.NewPanel();
        MainLayout = RestoreLayout(this, saved.Layout, RestoredDocumentsPanel);
        KeptView.Keep(CaptureViewSources(this));
    }

    /// <summary>The strings of the language the screen speaks now, for what reports to the app state later.</summary>
    private Strings ReadStrings() => Strings;

    public UiTheme Theme { get; set; } = UiTheme.Dark;
    public CodeViewPalette CodeViewPalette { get; set; } = CodeViewPalette.Dark;
    public NodeIconPalette NodeIconPalette { get; set; } = NodeIconPalette.Dark;
    public Strings Strings { get; set; } = Strings.En;

    /// <summary>What the screen asked of the app state while it drew, for the app to run once the frame is drawn.</summary>
    public Queue<AppCommand> AppCommands { get; } = new();

    /// <summary>Where the views and the open documents are docked, all as tabs: side by side,
    /// stacked or in floating windows.</summary>
    public MainLayout MainLayout { get; }

    public DockLayout Layout => MainLayout.Dock;

    /// <summary>The document panel the layout was restored with in the place of the documents, which
    /// shows the app state's first tab.</summary>
    public string RestoredDocumentsPanel { get; }

    /// <summary>How each open tab is shown, and the panel it is docked as.</summary>
    public DocumentViews Documents { get; } = new();

    /// <summary>The app state's active tab as the screen last followed it, which the views open beside.</summary>
    public DocumentTab? ActiveTab;

    /// <summary>When to try focusing another tab while none is active.</summary>
    public FocusRetry NextTabFocus { get; } = new();

    /// <summary>The language and options the tabs in front were last brought up to date with.</summary>
    public ShownDecompilation ShownDecompilation { get; } = new();

    /// <summary>Set while the user is asked whether to reset the layout.</summary>
    public bool ConfirmingLayoutReset;

    /// <summary>The browse tree over the open assemblies, with the view's expansion and selection.</summary>
    public BrowseTree Tree { get; } = new();

    /// <summary>Which assembly list the tree last showed.</summary>
    public ShownList ShownList { get; } = new();

    /// <summary>The node the tree's context menu was opened on.</summary>
    public BrowseNode? MenuTarget;

    /// <summary>The analyzer's tree as its view shows it.</summary>
    public AnalyzerRows Analyzer { get; }

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

    /// <summary>The decompiler options panel's folded groups.</summary>
    public FoldedGroups FoldedOptionGroups { get; }

    /// <summary>How the documents draw code: its size and the aids drawn around it.</summary>
    public DisplayOptions Display;

    /// <summary>What the settings last kept of the screen.</summary>
    public KeptView KeptView { get; } = new();

    public BookmarksPanel BookmarksPanel { get; } = new();

    /// <summary>The search panel's query, results and selection.</summary>
    public Search Search { get; }

    /// <summary>Set by <see cref="ScreenLayout.OpenSearch"/> until the search box takes focus.</summary>
    public bool IsSearchFocusPending;

    /// <summary>The metadata grids' flags filters.</summary>
    public MetadataFilters MetadataFilters { get; }

    /// <summary>The run of the export selected in the exports panel, or 0; one that is gone is as none.</summary>
    public int SelectedExport;

    /// <summary>The run of the export the exports panel's context menu was opened on, or 0.</summary>
    public int ExportMenuTarget;

    public ErrorDialog ErrorDialog { get; } = new();

    /// <summary>What the assembly lists dialog shows, and whether it is open.</summary>
    public AssemblyListsDialog ListsDialog { get; } = new();

    /// <summary>The "open from framework" dialog.</summary>
    public FrameworkPicker Picker { get; }

    /// <summary>What the export dialog asks for, and whether it is open.</summary>
    public ExportSetup ExportSetup { get; } = new();
}
