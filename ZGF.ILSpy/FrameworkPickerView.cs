using System.Diagnostics;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using ZGF.KeyboardModule;
using static ZGF.ILSpy.Assemblies;

namespace ZGF.ILSpy;

internal static class FrameworkPickerView
{
    public static void Render(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        var picker = ui.State.Picker;
        if (picker.IsOpen)
            picker.Update(appState.Assemblies);

        var theme = ui.Theme;
        var open = false;
        var close = false;
        var card = new Box { Width = Sizing.Grow(0f, AppLayout.PickerWidth), Height = Sizing.Percent(AppLayout.PickerHeight) };
        using (var dialog = ui.Dialog("picker", t.PickerTitle, ref picker.IsOpen, card))
        {
            if (!dialog.IsOpen) return;

            var listId = ui.GetId("picker-list");
            if (!ui.HasFocus(listId))
                MoveSelectionFromFilter(ui, picker, listId);

            if (picker.TakeFilterFocus())
                ui.Focus(ui.GetId("picker-filter"));
            open |= ui.TextField("picker-filter", ref picker.Filter, t.PickerFilter);

            using (ui.Panel(new Box { Height = Sizing.Grow(), Padding = theme.BorderWidth, Background = theme.Field, Clip = true }))
            {
                switch (picker.Scan)
                {
                    case RuntimeScan.NotStarted:
                    case RuntimeScan.Scanning:
                        ui.Text(t.PickerScanning, new Box { Padding = new Spacing(theme.ShellPaddingX, theme.Gap) }, color: theme.TextMuted);
                        break;
                    case RuntimeScan.Failed:
                        ui.Text(t.PickerScanFailed, new Box { Padding = new Spacing(theme.ShellPaddingX, theme.Gap) },
                            color: theme.TextMuted, wrap: true);
                        break;
                    case RuntimeScan.Scanned:
                        open |= Rows(ui, picker, listId);
                        break;
                    default:
                        throw new UnreachableException();
                }
            }

            using (ui.Row(new Box { Gap = theme.GapTight }))
            {
                var counts = (Shown: picker.Filtered.Count, Selected: picker.Selection.Count, t);
                var summary = ui.Memo("picker-count", counts, static a => Summary(a.t, a.Shown, a.Selected));
                ui.Text(summary, color: theme.TextMuted, size: theme.FontSizeSmall);
                ui.Spacer();
                close |= ui.Button(t.PickerCancel, id: "picker-cancel");
                open |= ui.Button(t.PickerOpen, enabled: picker.HasSelection, id: "picker-open", variant: ButtonVariant.Primary);
            }
        }

        if (open && picker.HasSelection)
        {
            OpenSelected(appState, picker);
            close = true;
        }
        if (close)
            picker.Close();
    }

    private static string Summary(Strings t, int shown, int selected) => $"{t.PickerCount(shown)} · {t.PickerSelected(selected)}";

    private static void OpenSelected(AppState appState, FrameworkPicker picker)
    {
        var paths = picker.ListSelected()
            .Select(a => a.FilePath);
        OpenAssemblies(appState, paths);
    }

    /// <summary>Walks the list from the filter, leaving Home and End to its text.</summary>
    private static void MoveSelectionFromFilter(Ui<UiState> ui, FrameworkPicker picker, int listId)
    {
        var page = ui.MeasureListPage(listId, ui.Theme.RowHeight);
        foreach (var stroke in ui.Input.Keys)
        {
            var movesCaret = stroke.Key is KeyboardKey.Home or KeyboardKey.End;
            if (!movesCaret)
                UiWidgets.ApplyListKey(stroke, picker.Selection, picker.Filtered, page);
        }
    }

    private static bool Rows(Ui<UiState> ui, FrameworkPicker picker, int listId)
    {
        var theme = ui.Theme;
        using var list = ui.SelectableList(listId, picker.Filtered, picker.Selection);
        for (var i = list.First; i < list.End; i++)
        {
            var assembly = picker.Filtered[i];
            using (ui.Id(assembly.FilePath.Text))
            using (var row = list.Row(i, ui.GetId("row"), new Box { Gap = theme.GapTight }))
            {
                var nameColor = row.Selected ? theme.SelectionText : theme.Text;
                NodeIcons.Draw(ui, NodeKind.Assembly);
                using (ui.Row(new Box { Gap = theme.Gap, AlignY = Align.Center }))
                {
                    ui.Text(assembly.Name, new Box { Width = Sizing.Grow() }, color: nameColor, overflow: TextOverflow.Ellipsis);
                    ui.Text(assembly.Framework, color: theme.TextMuted, size: theme.FontSizeSmall);
                }
            }
        }
        return list.Activated;
    }
}
