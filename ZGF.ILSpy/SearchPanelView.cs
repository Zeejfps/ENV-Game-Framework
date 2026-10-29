using System.Diagnostics;
using System.Runtime.CompilerServices;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Navigation;
using static ZGF.ILSpy.ScreenLayout;

namespace ZGF.ILSpy;

internal static class SearchPanelView
{
    /// <summary>The results list's focus and scroll id.</summary>
    public const int ResultsId = 0x53524C54;

    private static readonly SearchKinds[] Kinds =
    [
        SearchKinds.Types, SearchKinds.Methods, SearchKinds.Fields, SearchKinds.Properties, SearchKinds.Events, SearchKinds.Literals,
        SearchKinds.Tokens, SearchKinds.Resources, SearchKinds.Assemblies, SearchKinds.Namespaces,
    ];

    private static readonly ConditionalWeakTable<Strings, string[]> Summaries = new();
    private static readonly ConditionalWeakTable<Strings, string[]> Headers = new();

    public static void Render(Ui<UiState> ui, AppState appState)
    {
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
        using (ui.PaneToolbar(new Box { Gap = ui.Theme.Gap }))
        {
            KindsButton(ui, appState);
            Field(ui, appState);
            ResultCount(ui, ui.State.Search);
        }
        ui.Separator();
        Results(ui, appState);
    }

    private static void KindsButton(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        var kinds = ui.State.Search.Kinds;
        var summary = ui.Memo("search-kinds", (kinds, t), static a => Summary(a.t, a.kinds));
        var width = ui.SelectButtonWidth(Summaries.GetValue(t, ListAllSummaries));
        var tooltip = ui.Memo("search-kinds-tip", (kinds, t), static a => Description(a.t, a.kinds));
        using var menu = ui.SelectButton("search-kinds", summary, width, tooltip);
        if (menu.IsOpen)
            KindsMenu(ui, appState);
    }

    private static string Summary(Strings t, SearchKinds kinds)
    {
        switch (kinds)
        {
            case SearchKinds.All:
                return t.SearchKindsAll;
            case SearchKinds.TypesAndMembers:
                return t.SearchModeTypesAndMembers;
            case SearchKinds.Members:
                return t.SearchModeMembers;
        }
        var picked = ListPicked(kinds);
        if (picked.Length == 1)
            return KindLabel(t, picked[0]);
        return t.SearchKindsCount(picked.Length);
    }

    private static SearchKinds[] ListPicked(SearchKinds kinds)
    {
        return Kinds
            .Where(k => (kinds & k) != 0)
            .ToArray();
    }

    private static string[] ListAllSummaries(Strings t)
    {
        var summaries = new List<string>
        {
            t.SearchKindsAll, t.SearchModeTypesAndMembers, t.SearchModeMembers, t.SearchKindsCount(Kinds.Length),
        };
        foreach (var kind in Kinds)
            summaries.Add(KindLabel(t, kind));
        return summaries.ToArray();
    }

    private static string Description(Strings t, SearchKinds kinds)
    {
        if (kinds == SearchKinds.All)
            return $"{t.SearchMode}: {t.SearchKindsAll}";
        var labels = ListPicked(kinds).Select(k => KindLabel(t, k));
        return $"{t.SearchMode}:\n{string.Join("\n", labels)}";
    }

    /// <summary>A check item for all kinds over one per kind, kept open so several can be flipped
    /// in a row. While all is checked no kind is: checking a kind then picks it alone, and
    /// unchecking the last one picked checks all again.</summary>
    private static void KindsMenu(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        var search = ui.State.Search;
        var all = search.Kinds == SearchKinds.All;
        var kinds = search.Kinds;
        var checkAll = all;
        var palette = ui.State.NodeIconPalette;
        var allIcon = new UiIcon(ui.State.Icons.SearchAllKinds, ui.Theme.TextMuted);
        if (ui.MenuItem(t.SearchModeAll, ref checkAll, keepOpen: true, icon: allIcon))
            kinds = SearchKinds.All;
        ui.MenuSeparator();
        foreach (var kind in Kinds)
        {
            var on = !all && (kinds & kind) != 0;
            var icon = NodeIcons.Icon(KindNode(kind), NodeAccessibility.None, palette, ui.State.Icons);
            if (ui.MenuItem(KindLabel(t, kind), ref on, keepOpen: true, icon: icon))
                kinds = Toggle(kinds, kind, all, on);
        }
        if (kinds == search.Kinds)
            return;
        search.Kinds = kinds;
    }

    private static SearchKinds Toggle(SearchKinds kinds, SearchKinds kind, bool all, bool on)
    {
        if (all)
            return kind;
        if (on)
            return kinds | kind;
        var left = kinds & ~kind;
        if (left == SearchKinds.None)
            return SearchKinds.All;
        return left;
    }

    private static void Field(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        var search = ui.State.Search;
        var fieldId = ui.GetId("search");
        if (TakeSearchFocus(ui.State))
            ui.Focus(fieldId);
        var width = Sizing.Grow(AppLayout.SearchMinWidth, AppLayout.SearchWidth);
        var submitted = ui.TextField("search", ref search.Query, t.SearchField, width, ui.State.Icons.SearchField);
        ui.Tooltip(fieldId, ui.Memo("search-syntax", t, static s => s.SearchSyntaxTooltip(Shortcuts.Search.Label)));
        if (!ui.HasFocus(fieldId))
            return;
        if (submitted)
        {
            ShowSelected(appState, search);
            return;
        }
        if (ui.Input.IsKeyPressed(KeyboardKey.DownArrow) && search.Rows.Count > 0)
            ui.Focus(ResultsId, takesText: false);
    }

    private static void ShowSelected(AppState appState, Search search)
    {
        if (search.SelectedRow == null)
            search.Move(1);
        var row = search.SelectedRow;
        if (row != null)
            ShowNode(appState, row.Node);
    }

    private static void ResultCount(Ui<UiState> ui, Search search)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var count = ui.Memo("search-count", (search.Rows.Count, search.IsBusy, t), static a => CountText(a.t, a.Count, a.IsBusy));
        ui.Text(count, color: theme.TextMuted, size: theme.FontSizeSmall);
    }

    private static string CountText(Strings t, int count, bool isBusy)
    {
        if (isBusy)
            return t.SearchResultsSoFar(count);
        return t.SearchResults(count);
    }

    private static void Results(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        var search = ui.State.Search;
        var headers = Headers.GetValue(t, static t => [t.SearchColumnName, t.SearchColumnLocation, t.SearchColumnAssembly]);
        var sort = new TableSort(ColumnOf(search.Sort), SortIcon(ui.State.Icons, search));
        search.Selection.ShowIn(search.Rows);
        var table = ui.Table(ResultsId, ui.State.SearchColumns, search.Selection.Rows);
        var clicked = table.Header(headers, sort);
        if (clicked >= 0)
            search.SortBy(SortOf(clicked));
        ResultRows(ui, appState, table);
    }

    private static int ColumnOf(SearchSort sort) => (int)sort - 1;

    private static UiIcon SortIcon(AppIcons icons, Search search)
    {
        var glyph = search.Descending ? icons.SortDescending : icons.SortAscending;
        return new UiIcon(glyph);
    }

    private static SearchSort SortOf(int column) => (SearchSort)(column + 1);

    private static void ResultRows(Ui<UiState> ui, AppState appState, TableFrame table)
    {
        var search = ui.State.Search;
        using var body = table.Body(search.Rows.Count);
        search.Selection.TakeFrom(search.Rows);
        for (var i = body.First; i < body.End; i++)
            ResultRow(ui, appState, body, i);
        search.Selection.TakeFrom(search.Rows);
        var selected = search.SelectedRow;
        if (body.Activated && selected != null)
            ShowNode(appState, selected.Node, body.InNewTab);
    }

    private static void ResultRow(Ui<UiState> ui, AppState appState, TableBody body, int index)
    {
        var theme = ui.Theme;
        var result = ui.State.Search.Rows[index];
        var node = result.Node;
        var row = body.Row(index, RuntimeHelpers.GetHashCode(node));
        using (row)
        {
            if (row.Interaction.Clicked)
                ShowNode(appState, node);
            NameCell(ui, row, result);
            row.TextCell(1, result.Location, theme.TextMuted, overflow: TextOverflow.EllipsisMiddle);
            row.TextCell(2, NodeText.AssemblyLabel(node.Assembly), theme.TextMuted);
        }
        ui.Tooltip(row.Id, NodeText.Tooltip(node));
    }

    private static void NameCell(Ui<UiState> ui, TableRow row, SearchResult result)
    {
        var theme = ui.Theme;
        var node = result.Node;
        var color = row.Selected ? theme.SelectionText : theme.Text;
        using (row.Cell(0))
        {
            NodeIcons.Draw(ui, node);
            ui.Text(result.Text, new Box { Width = Sizing.Grow() }, color: color, overflow: TextOverflow.Ellipsis);
        }
    }

    private static string KindLabel(Strings t, SearchKinds kind) => kind switch
    {
        SearchKinds.Types => t.SearchModeTypes,
        SearchKinds.Methods => t.SearchModeMethods,
        SearchKinds.Fields => t.SearchModeFields,
        SearchKinds.Properties => t.SearchModeProperties,
        SearchKinds.Events => t.SearchModeEvents,
        SearchKinds.Literals => t.SearchModeLiterals,
        SearchKinds.Tokens => t.SearchModeToken,
        SearchKinds.Resources => t.SearchModeResources,
        SearchKinds.Assemblies => t.SearchModeAssemblies,
        SearchKinds.Namespaces => t.SearchModeNamespaces,
        _ => throw new UnreachableException(),
    };

    private static NodeIcon KindNode(SearchKinds kind) => kind switch
    {
        SearchKinds.Types => NodeIcon.Class,
        SearchKinds.Methods => NodeIcon.Method,
        SearchKinds.Fields => NodeIcon.Field,
        SearchKinds.Properties => NodeIcon.Property,
        SearchKinds.Events => NodeIcon.Event,
        SearchKinds.Literals => NodeIcon.Constant,
        SearchKinds.Tokens => NodeIcon.MetadataTable,
        SearchKinds.Resources => NodeIcon.Resource,
        SearchKinds.Assemblies => NodeIcon.Assembly,
        SearchKinds.Namespaces => NodeIcon.Namespace,
        _ => throw new UnreachableException(),
    };
}
