using ZGF.Desktop;
using System.Runtime.CompilerServices;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

internal static class SearchPanelView
{
    /// <summary>The results list's focus and scroll id.</summary>
    public const int ResultsId = 0x53524C54;

    private const string SearchShortcut = "Ctrl+Shift+F";

    private static readonly SearchKinds[] Kinds =
    [
        SearchKinds.Types, SearchKinds.Methods, SearchKinds.Fields, SearchKinds.Properties, SearchKinds.Events, SearchKinds.Literals,
        SearchKinds.Tokens, SearchKinds.Resources, SearchKinds.Assemblies, SearchKinds.Namespaces,
    ];

    private static readonly ConditionalWeakTable<Strings, string[]> Summaries = new();
    private static readonly ConditionalWeakTable<Strings, string[]> Headers = new();

    public static void Render(Ui ui, Workspace w, Strings t)
    {
        var theme = ui.Theme;
        var search = w.Search;
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
        using (ui.PaneToolbar(new Box { Gap = theme.Gap }))
        {
            using (var menu = ui.SelectButton("search-kinds", ui.Memo("search-kinds", (search.Kinds, t), static a => Summary(a.t, a.Kinds)),
                       ui.SelectButtonWidth(Summaries.GetValue(t, AllSummaries)), ui.Memo("search-kinds-tip", (search.Kinds, t), static a => Description(a.t, a.Kinds))))
            {
                if (menu.IsOpen)
                    KindsMenu(ui, w, t);
            }
            Field(ui, w, t);
            ui.Text(ui.Memo("search-count", (search.Rows.Count, search.IsBusy, t), static a => a.IsBusy ? a.t.SearchResultsSoFar(a.Count) : a.t.SearchResults(a.Count)),
                color: theme.TextMuted, size: theme.FontSizeSmall);
        }
        ui.Separator();

        var rowHeight = theme.RowHeight;
        var area = ui.Interact(ResultsId);
        if (area.Pressed)
            ui.Focus(ResultsId, takesText: false);
        var focused = ui.HasFocus(ResultsId);
        if (focused)
            Keys(ui, w, rowHeight);

        var sortIcon = new UiIcon(search.Descending ? LucideIcons.ChevronDown : LucideIcons.ChevronUp, LucideIcons.Family);
        var clicked = ui.TableHeader("header", w.SearchColumns, ResultsId,
            Headers.GetValue(t, static t => [t.SearchColumnName, t.SearchColumnLocation, t.SearchColumnAssembly]), theme.ShellPaddingX,
            sort: new TableSort((int)search.Sort - 1, sortIcon));
        if (clicked >= 0)
            search.SortBy((SearchSort)(clicked + 1));
        using var list = ui.VirtualScroll(ResultsId, search.Rows.Count, rowHeight, new Box { Gap = 0f });
        if (search.ScrollToSelected && list.ScrollIntoView(search.Selected))
            search.ScrollToSelected = false;
        for (var i = list.First; i < list.End; i++)
        {
            var row = search.Rows[i];
            var node = row.Result.Node;
            using (ui.Id(RuntimeHelpers.GetHashCode(node)))
            {
                var item = ui.Interact("row");
                var newTab = item.ClickedWith(PointerButton.Middle);
                if (item.Pressed)
                    ui.Focus(ResultsId, takesText: false);
                if (item.Clicked || newTab)
                {
                    search.Select(i);
                    w.Show(node, newTab);
                }

                var selected = i == search.Selected;
                var background = selected ? focused ? theme.Selection : theme.SelectionInactive : item.Hovered ? theme.RowHover : 0u;
                var columns = w.SearchColumns;
                using (ui.Row(item.Id, new Box
                {
                    Height = rowHeight,
                    Gap = 0f,
                    Padding = new Spacing(theme.ShellPaddingX, 0f),
                    AlignY = Align.Center,
                    Background = background,
                }))
                {
                    using (ui.TableCell(columns, 0))
                    {
                        NodeIcons.Draw(ui, node.Kind, node.Accessibility);
                        ui.Text(row.Result.Text, new Box { Width = Sizing.Grow() }, color: selected ? theme.SelectionText : theme.Text, overflow: TextOverflow.Ellipsis);
                    }
                    using (ui.TableCell(columns, 1))
                        ui.Text(row.Location, new Box { Width = Sizing.Grow() }, color: theme.TextMuted, overflow: TextOverflow.EllipsisMiddle);
                    using (ui.TableCell(columns, 2))
                        ui.Text(row.Assembly, new Box { Width = Sizing.Grow() }, color: theme.TextMuted, overflow: TextOverflow.Ellipsis);
                }
                ui.Tooltip(item.Id, NodeText.Tooltip(node));
            }
        }
    }

    private static void Field(Ui ui, Workspace w, Strings t)
    {
        var search = w.Search;
        var fieldId = ui.GetId("search");
        if (w.TakeSearchFocus())
            ui.Focus(fieldId);
        var submitted = ui.TextField("search", ref search.Query, t.SearchField, Sizing.Grow(AppLayout.SearchMinWidth, AppLayout.SearchWidth),
            LucideIcons.Search, LucideIcons.Family);
        ui.Tooltip(fieldId, ui.Memo("search-syntax", t, static s => s.SearchSyntaxTooltip(SearchShortcut)));
        if (!ui.HasFocus(fieldId)) return;
        if (submitted)
        {
            if (search.Selected < 0)
                search.Move(1);
            if (search.SelectedRow is { } row)
                w.Show(row.Result.Node);
        }
        else if (ui.Input.IsKeyPressed(KeyboardKey.DownArrow) && search.Rows.Count > 0)
        {
            ui.Focus(ResultsId, takesText: false);
        }
    }

    /// <summary>A check item for all kinds over one per kind, kept open so several can be flipped
    /// in a row. While all is checked no kind is: checking a kind then picks it alone, and
    /// unchecking the last one picked checks all again.</summary>
    private static void KindsMenu(Ui ui, Workspace w, Strings t)
    {
        var search = w.Search;
        var all = search.Kinds == SearchKinds.All;
        var kinds = search.Kinds;
        var checkAll = all;
        if (ui.MenuItem(t.SearchModeAll, ref checkAll, keepOpen: true))
            kinds = SearchKinds.All;
        ui.MenuSeparator();
        foreach (var kind in Kinds)
        {
            var on = !all && (kinds & kind) != 0;
            if (!ui.MenuItem(KindLabel(t, kind), ref on, keepOpen: true)) continue;
            kinds = all ? kind : on ? kinds | kind : kinds & ~kind;
            if (kinds == SearchKinds.None)
                kinds = SearchKinds.All;
        }
        if (kinds == search.Kinds) return;
        search.Kinds = kinds;
        w.SaveSettings();
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
        var picked = Kinds.Where(k => (kinds & k) != 0).ToArray();
        return picked.Length == 1 ? KindLabel(t, picked[0]) : t.SearchKindsCount(picked.Length);
    }

    private static string[] AllSummaries(Strings t) =>
    [
        t.SearchKindsAll, t.SearchModeTypesAndMembers, t.SearchModeMembers, t.SearchKindsCount(Kinds.Length),
        ..Kinds.Select(k => KindLabel(t, k)),
    ];

    private static string Description(Strings t, SearchKinds kinds) =>
        kinds == SearchKinds.All
            ? $"{t.SearchMode}: {t.SearchKindsAll}"
            : $"{t.SearchMode}:\n{string.Join("\n", Kinds.Where(k => (kinds & k) != 0).Select(k => KindLabel(t, k)))}";

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
        _ => kind.ToString(),
    };

    private static void Keys(Ui ui, Workspace w, float rowHeight)
    {
        var search = w.Search;
        var page = ui.TryGetRect(ResultsId, out var view) ? Math.Max(1, (int)(view.Height / rowHeight) - 1) : 10;
        var keys = ui.Input.Keys;
        for (var k = 0; k < keys.Count; k++)
        {
            var stroke = keys[k];
            if ((stroke.Modifiers & (KeyModifiers.Alt | KeyModifiers.Super)) != 0) continue;
            switch (stroke.Key)
            {
                case KeyboardKey.UpArrow:
                    search.Move(-1);
                    break;
                case KeyboardKey.DownArrow:
                    search.Move(1);
                    break;
                case KeyboardKey.PageUp:
                    search.Move(-page);
                    break;
                case KeyboardKey.PageDown:
                    search.Move(page);
                    break;
                case KeyboardKey.Home:
                    search.Move(-search.Rows.Count);
                    break;
                case KeyboardKey.End:
                    search.Move(search.Rows.Count);
                    break;
                case KeyboardKey.Enter when search.SelectedRow is { } selected:
                    w.Show(selected.Result.Node, inNewTab: (stroke.Modifiers & KeyModifiers.Control) != 0);
                    break;
            }
        }
    }
}
