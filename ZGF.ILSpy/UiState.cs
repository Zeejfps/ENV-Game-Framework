using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The screen, one per app: what every view is drawn with this frame (the theme, the editor's
/// and icons' colors, the strings of the chosen language), and the state that exists only because there
/// is a screen: the layout, each tab's view, the trees' and tables' state, the panels and dialogs with
/// what is selected in them, and UI flags. It and the app state never see each other: the app hands it
/// what it shows each frame and runs what it asks of the app state through <see cref="AppCommands"/>.</summary>
public sealed partial class UiState : IUiState
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
        Analyzer = new AnalyzerRows(AppCommands);
        _restoredDocuments = NewDocumentPanel();
        MainLayout = RestoreLayout(saved.Layout, _restoredDocuments);
        _keptView = CaptureViewSources();
    }

    /// <summary>The strings of the language the screen speaks now, for what reports to the app state later.</summary>
    private Strings ReadStrings() => Strings;

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

    /// <summary>Takes the colors of <paramref name="theme"/> and the strings of <paramref name="locale"/>.</summary>
    public void ApplyThemeAndLanguage(AppThemeId theme, Locale locale)
    {
        var colors = AppThemes.For(theme);
        Theme = colors.Ui;
        CodeViewPalette = colors.Code;
        NodeIconPalette = colors.Icons;
        Strings = Strings.For(locale);
    }
}
