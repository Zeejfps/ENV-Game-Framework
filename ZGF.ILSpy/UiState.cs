using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The screen over a <see cref="Workspace"/>, one per app: what every view is drawn with this
/// frame (the theme, the editor's and icons' colors, the strings of the chosen language), and the state
/// that exists only because there is a screen: each tab's view, the tables' columns, the panels and
/// dialogs with what is selected in them, and UI flags. The workspace never sees it; it asks the
/// screen through <see cref="Workspace.ViewAsks"/>.</summary>
public sealed partial class UiState : IUiState
{
    private readonly Workspace _workspace;

    public UiState(Workspace workspace)
    {
        _workspace = workspace;
        var settings = workspace.Settings;
        FoldedOptionGroups = new FoldedGroups(settings.FoldedOptionGroups);
        BookmarkColumns.Restore(settings.BookmarkColumns);
        SearchColumns.Restore(settings.SearchColumns);
        ExportColumns.Restore(settings.ExportColumns);
        Search = new Search(ReadStrings, workspace.ReportError);
        Search.Kinds = settings.SearchKinds;
        MetadataFilters = new MetadataFilters(settings.FlagsFilters);
        Picker = new FrameworkPicker(ReadStrings, workspace.ReportError);
        ExportSetup.RestoreWindow(settings.ExportWindow);
        workspace.Layout.PanelClosed += OnPanelClosed;
        AddOpenTabViews();
        _keptView = CaptureViewSources();
    }

    /// <summary>The strings of the language the workspace speaks now, for what reports to it later.</summary>
    private Strings ReadStrings() => Strings.For(_workspace.Locale);

    public UiTheme Theme { get; set; } = UiTheme.Dark;
    public CodeViewPalette CodeViewPalette { get; set; } = CodeViewPalette.Dark;
    public NodeIconPalette NodeIconPalette { get; set; } = NodeIconPalette.Dark;
    public Strings Strings { get; set; } = Strings.En;

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

    public BookmarksPanel BookmarksPanel { get; } = new();

    /// <summary>The search panel's query, results and selection.</summary>
    public Search Search { get; }

    /// <summary>Set by <see cref="OpenSearch"/> until the search box takes focus.</summary>
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

    /// <summary>Does what comes before drawing a frame: takes the theme and strings the workspace's
    /// preferences pick, follows the open tabs, and does what the features asked of the view.</summary>
    public void StartFrame()
    {
        ApplyThemeAndLanguage();
        SyncTabViews();
        TakeViewAsks();
    }

    private void ApplyThemeAndLanguage()
    {
        var theme = AppThemes.For(_workspace.Theme);
        Theme = theme.Ui;
        CodeViewPalette = theme.Code;
        NodeIconPalette = theme.Icons;
        Strings = Strings.For(_workspace.Locale);
    }

    /// <summary>Does what comes after drawing a frame: tells the tabs where their views stand, and
    /// hands the workspace what its settings keep of the screen when that changed.</summary>
    public void FinishFrame()
    {
        KeepTabPositions();
        KeepViewSettings();
    }
}
