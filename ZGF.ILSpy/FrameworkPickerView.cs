using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy;

internal static class FrameworkPickerView
{
    public static void Render(Ui ui, Workspace w)
    {
        var picker = w.Picker;
        if (picker.IsOpen)
            picker.Update(w.Assemblies);

        var theme = ui.Theme;
        var open = false;
        var close = false;
        using (var dialog = ui.Dialog("picker", "Open from framework", ref picker.IsOpen, new Box { Width = 560f, Height = Sizing.Percent(0.75f) }))
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
            open |= ui.TextField("picker-filter", ref picker.Filter, "Filter by name");

            using (ui.Box(new Box
            {
                Width = Sizing.Grow(),
                Height = Sizing.Grow(),
                Background = theme.Field,
                BorderColor = theme.Border,
                BorderWidth = 1f,
                Radius = theme.CornerRadius,
                Padding = 2f,
            }))
            {
                if (picker.IsLoading)
                    ui.Text("Scanning the runtime…", new Box { Padding = 8f }, color: theme.TextMuted);
                else if (picker.Error != null)
                    ui.Text("The runtime could not be scanned; see the error in the status bar.", new Box { Padding = 8f }, color: theme.TextMuted);
                else
                    open |= Rows(ui, picker);
            }

            using (ui.Row())
            {
                ui.Text(ui.Memo("picker-count", picker.Filtered.Count, static n => $"{n} assemblies"), color: theme.TextMuted);
                ui.Spacer();
                open |= ui.Button("Open", enabled: picker.Selected != null, id: "picker-open");
                close |= ui.Button("Cancel", id: "picker-cancel");
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

    private static bool Rows(Ui ui, FrameworkPickerModel picker)
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
                var background = selected ? theme.Selection : row.Hovered ? theme.Hovered(theme.Field) : 0u;
                using (ui.Row(row.Id, new Box { Height = rowHeight, Gap = 8f, Padding = new Spacing(8f, 0f), Background = background }))
                {
                    var listed = picker.IsListed(assembly);
                    ui.Text(assembly.Name, color: listed ? theme.TextMuted : theme.Text);
                    ui.Spacer();
                    ui.Text(listed ? "open" : assembly.Framework, color: theme.TextMuted);
                }
            }
        }
        if (picker.ScrollToSelected && list.ScrollIntoView(picker.SelectedIndex))
            picker.ScrollToSelected = false;
        return open;
    }
}
