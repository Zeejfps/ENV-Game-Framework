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

            var keys = ui.Input.Keys;
            for (var i = 0; i < keys.Count; i++)
            {
                switch (keys[i].Key)
                {
                    case KeyboardKey.UpArrow: picker.Move(-1); break;
                    case KeyboardKey.DownArrow: picker.Move(1); break;
                    case KeyboardKey.PageUp: picker.Move(-10); break;
                    case KeyboardKey.PageDown: picker.Move(10); break;
                }
            }

            if (picker.FocusFilter)
            {
                picker.FocusFilter = false;
                ui.Focus(ui.GetId("picker-filter"));
            }
            open |= ui.TextField("picker-filter", ref picker.Filter, t.PickerFilter);

            using (ui.Box(new Box
            {
                Width = Sizing.Grow(),
                Height = Sizing.Grow(),
                Background = theme.Field,
                BorderColor = theme.Border,
                BorderWidth = theme.BorderWidth,
                Radius = theme.PanelRadius,
                Padding = theme.BorderWidth,
                Clip = true,
            }))
            {
                if (picker.IsLoading)
                    ui.Text(t.PickerScanning, new Box { Padding = new Spacing(theme.ShellPaddingX, theme.Gap) }, color: theme.TextMuted);
                else if (picker.Error != null)
                    ui.Text(t.PickerScanFailed, new Box { Padding = new Spacing(theme.ShellPaddingX, theme.Gap) },
                        color: theme.TextMuted, wrap: true);
                else
                    open |= Rows(ui, picker, t);
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

    private static bool Rows(Ui ui, FrameworkPickerModel picker, Strings t)
    {
        var theme = ui.Theme;
        var rowHeight = theme.RowHeight;
        var open = false;
        using var list = ui.VirtualScroll("picker-list", picker.Filtered.Count, rowHeight, new Box { Gap = 0f });
        for (var i = list.First; i < list.End; i++)
        {
            var assembly = picker.Filtered[i];
            var selected = i == picker.SelectedIndex;
            using (ui.Id(assembly.FilePath))
            {
                var row = ui.Interact("row");
                if (row.Pressed)
                {
                    picker.SelectedIndex = i;
                    open |= row.DoubleClicked;
                }
                var background = selected ? theme.Selection : row.Hovered ? theme.RowHover : 0u;
                using (ui.Row(row.Id, new Box
                {
                    Height = rowHeight,
                    Gap = theme.GapTight,
                    Padding = new Spacing(theme.ShellPaddingX, 0f),
                    Background = background,
                }))
                {
                    var listed = picker.IsListed(assembly);
                    NodeIcons.Draw(ui, NodeKind.Assembly);
                    using (ui.Row(new Box { Gap = theme.Gap, AlignY = Align.Center }))
                    {
                        ui.Text(assembly.Name, new Box { Width = Sizing.Grow() }, color: listed ? theme.TextMuted : selected ? theme.SelectionText : theme.Text,
                            overflow: TextOverflow.Ellipsis);
                        ui.Text(listed ? t.PickerListed : assembly.Framework, color: theme.TextMuted, size: theme.FontSizeSmall);
                    }
                }
            }
        }
        if (picker.ScrollToSelected)
        {
            list.ScrollIntoView(picker.SelectedIndex);
            picker.ScrollToSelected = false;
        }
        return open;
    }
}
