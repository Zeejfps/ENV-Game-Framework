using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using ZGF.KeyboardModule;
using static ZGF.ILSpy.Comparisons;

namespace ZGF.ILSpy;

/// <summary>The "compare with" dialog: a filter over the open assemblies the one to compare can be compared
/// with, each with its version, framework and file; Enter, a double-click or Compare compares with the
/// selected one in a new tab.</summary>
internal static class ComparisonPickerView
{
    private const string DialogId = "compare-picker";
    private const string FilterId = "compare-picker-filter";
    private const string ListId = "compare-picker-list";

    public static void Render(Ui<UiState> ui, AppState appState)
    {
        var picker = ui.State.ComparisonPicker;
        if (picker.IsOpen)
            picker.Update(appState.Assemblies);
        var compared = picker.Compared;
        if (compared == null)
            return;
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var compare = false;
        var close = false;
        var card = new Box { Width = Sizing.Grow(0f, AppLayout.PickerWidth), Height = Sizing.Percent(AppLayout.PickerHeight) };
        using (var dialog = ui.Dialog(DialogId, t.ComparePickerTitle(compared.NameAndVersion), ref picker.IsOpen, card))
        {
            if (!dialog.IsOpen)
                return;
            var listId = ui.GetId(ListId);
            if (!ui.HasFocus(listId))
                MoveSelectionFromFilter(ui, picker, listId);
            if (picker.TakeFilterFocus())
                ui.Focus(ui.GetId(FilterId));
            compare |= ui.TextField(FilterId, ref picker.Filter, t.ComparePickerFilter);
            using (ui.Panel(new Box { Height = Sizing.Grow(), Padding = theme.BorderWidth, Background = theme.Field, Clip = true }))
                compare |= ListOrHint(ui, picker, listId);
            using (ui.Row(new Box { Gap = theme.GapTight }))
            {
                ui.Spacer();
                close |= ui.Button(t.PickerCancel, id: "compare-picker-cancel");
                compare |= ui.Button(t.ComparePickerCompare, enabled: picker.Selected != null, id: "compare-picker-compare", variant: ButtonVariant.Primary);
            }
        }
        if (compare)
            close |= CompareWithSelected(appState, picker, compared, t);
        if (close)
            picker.Close();
    }

    /// <summary>Walks the list from the filter, leaving Home and End to its text.</summary>
    private static void MoveSelectionFromFilter(Ui<UiState> ui, ComparisonPicker picker, int listId)
    {
        var page = ui.MeasureListPage(listId, ui.Theme.RowHeight);
        var count = picker.Filtered.Count;
        foreach (var stroke in ui.Input.Keys)
        {
            var movesCaret = stroke.Key is KeyboardKey.Home or KeyboardKey.End;
            if (movesCaret)
                continue;
            var index = picker.Selection.Index;
            if (UiWidgets.ApplyListKey(stroke, ref index, count, page))
                picker.Selection.Select(index, count);
        }
    }

    /// <summary>The assemblies offered, or why there are none. True when one was activated.</summary>
    private static bool ListOrHint(Ui<UiState> ui, ComparisonPicker picker, int listId)
    {
        if (picker.Filtered.Count > 0)
            return Rows(ui, picker, listId);
        var theme = ui.Theme;
        ui.Text(ui.State.Strings.ComparePickerNone, new Box { Padding = new Spacing(theme.ShellPaddingX, theme.Gap) }, color: theme.TextMuted, wrap: true);
        return false;
    }

    private static bool Rows(Ui<UiState> ui, ComparisonPicker picker, int listId)
    {
        using var list = ui.SelectableList(listId, picker.Filtered.Count, picker.Selection);
        for (var i = list.First; i < list.End; i++)
            Row(ui, list, i, picker.Filtered[i]);
        return list.Activated;
    }

    /// <summary>The assembly's name and version, then its framework and, muted, its file, shortened in the middle so its folder and name stay.</summary>
    private static void Row(Ui<UiState> ui, ListScope list, int index, LoadedAssembly assembly)
    {
        var theme = ui.Theme;
        using (ui.Id(assembly.FilePath))
        using (var row = list.Row(index, ui.GetId("row"), new Box { Gap = theme.GapTight, AlignY = Align.Center }))
        {
            var nameColor = row.Selected ? theme.SelectionText : theme.Text;
            NodeIcons.Draw(ui, NodeKind.Assembly);
            ui.Text(assembly.NameAndVersion, color: nameColor);
            FrameworkText(ui, assembly);
            ui.Text(assembly.FilePath, new Box { Width = Sizing.Grow() }, color: theme.TextMuted, size: theme.FontSizeSmall, overflow: TextOverflow.EllipsisMiddle);
        }
    }

    private static void FrameworkText(Ui<UiState> ui, LoadedAssembly assembly)
    {
        var moniker = assembly.FrameworkMoniker;
        if (moniker == null)
            return;
        ui.Text(moniker, color: ui.Theme.TextMuted, size: ui.Theme.FontSizeSmall);
    }

    /// <summary>Compares the assembly to compare with the one selected, if any; true when it did.</summary>
    private static bool CompareWithSelected(AppState appState, ComparisonPicker picker, LoadedAssembly compared, Strings t)
    {
        var other = picker.Selected;
        if (other == null)
            return false;
        CompareAssemblies(appState, compared, other, t.ComparisonTitle(compared.Name));
        return true;
    }
}
