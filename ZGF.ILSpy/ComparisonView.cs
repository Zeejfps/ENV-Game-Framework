using System.Diagnostics;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core.Comparison;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Comparisons;
using static ZGF.ILSpy.Navigation;

namespace ZGF.ILSpy;

/// <summary>A comparison's tab: which assemblies are compared and how much changed, over the tree of what
/// changed; its tools sit in the bar over the document. Activating an entry opens its code in the right
/// assembly, or in the left when the right lacks it; its menu opens either.</summary>
internal static class ComparisonView
{
    private const string TreeId = "comparison";
    private const string MenuId = "comparison-menu";
    private const string Arrow = "  →  ";

    public static void Render(Ui<UiState> ui, AppState appState, DocumentTab tab, AssemblyComparison comparison)
    {
        var rows = ui.State.Documents.ViewOf(tab).Comparison;
        rows.SetStyle(ui.Theme, ui.State.NodeIconPalette, ui.State.Icons, ui.State.Strings);
        rows.Follow(comparison.Diff);
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f, Background = ui.Theme.PaneBackground });
        if (!ui.State.Display.ShowOmnibar)
            OwnToolbar(ui, appState, tab, comparison);
        Summary(ui, comparison);
        var hint = FindHint(ui, comparison, rows);
        if (hint != null)
        {
            DocumentsView.Hint(ui, hint);
            return;
        }
        Tree(ui, appState, rows);
        ContextMenu(ui, appState, rows);
    }

    /// <summary>The tools in a bar of the tab's own, while the bar over documents, where they sit otherwise, is hidden.</summary>
    private static void OwnToolbar(Ui<UiState> ui, AppState appState, DocumentTab tab, AssemblyComparison comparison)
    {
        using (ui.PaneToolbar())
            Tools(ui, appState, tab, comparison);
        ui.Separator();
    }

    /// <summary>The comparison's tools, for the bar over the document: collapsing and expanding the tree,
    /// swapping the assemblies' sides, and what it shows.</summary>
    public static void Tools(Ui<UiState> ui, AppState appState, DocumentTab tab, AssemblyComparison comparison)
    {
        var t = ui.State.Strings;
        var icons = ui.State.Icons;
        var rows = ui.State.Documents.ViewOf(tab).Comparison;
        var any = rows.Roots.Count > 0;
        using (ui.Row(new Box { Gap = ui.Theme.Gap, AlignY = Align.Center }))
        {
            if (ui.IconButton("collapse", icons.AnalyzerCollapseAll, t.CodeCollapseAll, any))
                rows.CollapseAll();
            if (ui.IconButton("expand", icons.AnalyzerExpandAll, t.CodeExpandAll, any))
                rows.ExpandAll();
            ui.Separator(ui.Theme.IconSize);
            if (ui.IconButton("swap", icons.ComparisonSwap, t.ComparisonSwap))
                SwapComparedAssemblies(appState, comparison);
            ui.Separator(ui.Theme.IconSize);
            OnlyDifferencesCheckbox(ui, rows);
            HiddenMembersCheckbox(ui, appState, comparison);
        }
    }

    private static void OnlyDifferencesCheckbox(Ui<UiState> ui, ComparisonRows rows)
    {
        var only = rows.OnlyDifferences;
        if (ui.Checkbox(ui.State.Strings.ComparisonOnlyDifferences, ref only, id: "only-differences"))
            rows.ShowOnlyDifferences(only);
    }

    private static void HiddenMembersCheckbox(Ui<UiState> ui, AppState appState, AssemblyComparison comparison)
    {
        var all = comparison.Scope == ApiScope.AllDeclared;
        if (ui.Checkbox(ui.State.Strings.ComparisonShowHiddenMembers, ref all, id: "show-hidden-members"))
            ChangeComparedScope(appState, comparison, ScopeOf(all));
    }

    private static ApiScope ScopeOf(bool all)
    {
        if (all)
            return ApiScope.AllDeclared;
        return ApiScope.PublicApi;
    }

    /// <summary>Which assemblies were compared, the left one first, and how many types and members were
    /// added, removed and changed, each in the color its rows are drawn in, and whether they are being
    /// compared again; nothing before the first comparison finished.</summary>
    private static void Summary(Ui<UiState> ui, AssemblyComparison comparison)
    {
        var diff = comparison.Diff;
        if (diff == null)
            return;
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Row(new Box { Padding = new Spacing(theme.Gap, theme.GapTight), Gap = theme.Gap }))
        {
            ui.Text(diff.LeftName + Arrow + diff.RightName);
            ui.Text(t.ComparisonAddedCount(diff.Added), color: theme.Success);
            ui.Text(t.ComparisonRemovedCount(diff.Removed), color: theme.Danger);
            ui.Text(t.ComparisonChangedCount(diff.Changed), color: theme.Warning);
            if (comparison.IsComparing)
                ui.Text(t.ComparisonComparing, color: theme.TextMuted);
        }
        ui.Separator();
    }

    /// <summary>What the tab says in place of the tree: why the comparison failed, that it runs, or that
    /// nothing differs; null when it shows the tree.</summary>
    private static string? FindHint(Ui<UiState> ui, AssemblyComparison comparison, ComparisonRows rows)
    {
        var t = ui.State.Strings;
        var failure = comparison.Failure;
        if (failure != null)
            return ui.Memo("comparison-failure", (failure, t), static a => DescribeFailure(a.t, a.failure));
        if (comparison.Diff == null)
            return t.ComparisonComparing;
        if (rows.Roots.Count == 0)
            return t.ComparisonNoDifferences;
        return null;
    }

    private static string DescribeFailure(Strings t, ComparisonFailure failure) => failure switch
    {
        ComparisonFailure.AssemblyClosed closed => t.ComparisonAssemblyClosed(closed.File),
        ComparisonFailure.Failed failed => t.ErrorCompare(failed.Left, failed.Right),
        _ => throw new UnreachableException(),
    };

    private static void Tree(Ui<UiState> ui, AppState appState, ComparisonRows rows)
    {
        var box = new Box { Padding = new Spacing(0f, ui.Theme.GapTight) };
        var result = ui.TreeView(TreeId, rows.Roots, rows, rows.View, box);
        if (result.Activated)
            Open(appState, result.Node, inNewTab: false);
        if (result.ContextMenuRequested && result.Node != null)
            ui.OpenContextMenu(MenuId, result.ContextMenuAt);
    }

    /// <summary>Opens the code of <paramref name="entry"/> in the right assembly, else in the left.</summary>
    private static void Open(AppState appState, ApiEntry? entry, bool inNewTab)
    {
        if (entry == null)
            return;
        var side = RightOf(entry) ?? LeftOf(entry);
        if (side == null)
            return;
        Navigate(appState, side.Reference, inNewTab);
    }

    /// <summary>How the left assembly has <paramref name="entry"/>; null for a namespace or what it lacks.</summary>
    private static ApiSide? LeftOf(ApiEntry entry)
    {
        if (entry is not ApiEntry.Entity entity)
            return null;
        return entity.Sides switch
        {
            ApiSides.LeftOnly sides => sides.Left,
            ApiSides.Both sides => sides.Left,
            ApiSides.RightOnly => null,
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>How the right assembly has <paramref name="entry"/>; null for a namespace or what it lacks.</summary>
    private static ApiSide? RightOf(ApiEntry entry)
    {
        if (entry is not ApiEntry.Entity entity)
            return null;
        return entity.Sides switch
        {
            ApiSides.RightOnly sides => sides.Right,
            ApiSides.Both sides => sides.Right,
            ApiSides.LeftOnly => null,
            _ => throw new UnreachableException(),
        };
    }

    private static void ContextMenu(Ui<UiState> ui, AppState appState, ComparisonRows rows)
    {
        var t = ui.State.Strings;
        using var menu = ui.ContextMenu(MenuId);
        if (!menu.IsOpen)
            return;
        var entry = rows.View.Selected;
        if (entry == null)
            return;
        var left = LeftOf(entry);
        var right = RightOf(entry);
        if (ui.MenuItem(t.ComparisonOpenLeft, enabled: left != null, id: "open-left") && left != null)
            Navigate(appState, left.Reference, inNewTab: false);
        if (ui.MenuItem(t.ComparisonOpenRight, enabled: right != null, id: "open-right") && right != null)
            Navigate(appState, right.Reference, inNewTab: false);
    }
}
