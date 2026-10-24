using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The screen over a <see cref="Workspace"/>, one per app: what every view is drawn with this
/// frame (the theme, the editor's and icons' colors, the strings of the chosen language), and the state
/// that exists only because there is a screen: each tab's view, the tables' columns and the folded
/// groups. The workspace never sees it; it asks the screen through <see cref="Workspace.ViewAsks"/>.</summary>
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
        AddOpenTabViews();
        _keptView = CaptureViewSources();
    }

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
