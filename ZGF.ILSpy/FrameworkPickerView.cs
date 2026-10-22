using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy;

internal static class FrameworkPickerView
{
    public static void Render(Ui ui, Workspace w, Strings t)
    {
        var picker = w.Picker;
        if (picker.IsOpen)
            picker.Update(w.Assemblies);

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

            if (picker.FocusFilter)
            {
                picker.FocusFilter = false;
                ui.Focus(ui.GetId("picker-filter"));
            }
            open |= ui.TextField("picker-filter", ref picker.Filter, t.PickerFilter);

            using (ui.Panel(new Box { Height = Sizing.Grow(), Padding = theme.BorderWidth, Background = theme.Field, Clip = true }))
            {
                if (picker.IsLoading)
                    ui.Text(t.PickerScanning, new Box { Padding = new Spacing(theme.ShellPaddingX, theme.Gap) }, color: theme.TextMuted);
                else if (picker.Error != null)
                    ui.Text(t.PickerScanFailed, new Box { Padding = new Spacing(theme.ShellPaddingX, theme.Gap) },
                        color: theme.TextMuted, wrap: true);
                else
                    open |= Rows(ui, picker, t, listId);
            }

            using (ui.Row(new Box { Gap = theme.GapTight }))
            {
                ui.Text(ui.Memo("picker-count", (picker.Filtered.Count, t), static a => a.t.PickerCount(a.Count)), color: theme.TextMuted, size: theme.FontSizeSmall);
                ui.Spacer();
                close |= ui.Button(t.PickerCancel, id: "picker-cancel");
                open |= ui.Button(t.PickerOpen, enabled: picker.Selected != null, id: "picker-open", variant: ButtonVariant.Primary);
            }
        }

        if (open && picker.Selected is { } selected)
        {
            w.Open(selected.FilePath);
            close = true;
        }
        if (close)
            picker.Close();
    }

    /// <summary>Walks the list from the filter, leaving Home and End to its text.</summary>
    private static void MoveSelectionFromFilter(Ui ui, FrameworkPickerModel picker, int listId)
    {
        var count = picker.Filtered.Count;
        var page = ui.MeasureListPage(listId, ui.Theme.RowHeight);
        var index = picker.Selection.Index;
        foreach (var stroke in ui.Input.Keys)
        {
            var movesCaret = stroke.Key is KeyboardKey.Home or KeyboardKey.End;
            if (!movesCaret)
                UiWidgets.ApplyListKey(stroke, ref index, count, page);
        }
        if (index != picker.Selection.Index)
            picker.Selection.Select(index, count);
    }

    private static bool Rows(Ui ui, FrameworkPickerModel picker, Strings t, int listId)
    {
        var theme = ui.Theme;
        using var list = ui.SelectableList(listId, picker.Filtered.Count, picker.Selection);
        for (var i = list.First; i < list.End; i++)
        {
            var assembly = picker.Filtered[i];
            using (ui.Id(assembly.FilePath))
            using (var row = list.Row(i, ui.GetId("row"), new Box { Gap = theme.GapTight }))
            {
                var listed = picker.IsListed(assembly);
                var nameColor = NameColor(theme, listed, row.Selected);
                var note = listed ? t.PickerListed : assembly.Framework;
                NodeIcons.Draw(ui, NodeKind.Assembly);
                using (ui.Row(new Box { Gap = theme.Gap, AlignY = Align.Center }))
                {
                    ui.Text(assembly.Name, new Box { Width = Sizing.Grow() }, color: nameColor, overflow: TextOverflow.Ellipsis);
                    ui.Text(note, color: theme.TextMuted, size: theme.FontSizeSmall);
                }
            }
        }
        return list.Activated;
    }

    private static uint NameColor(UiTheme theme, bool listed, bool selected)
    {
        if (listed) return theme.TextMuted;
        if (selected) return theme.SelectionText;
        return theme.Text;
    }
}
