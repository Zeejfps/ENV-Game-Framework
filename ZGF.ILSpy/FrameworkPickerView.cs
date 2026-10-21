using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy;

internal static class FrameworkPickerView
{
    private const uint Scrim = 0x99000000;

    public static void Render(Ui ui, Workspace w)
    {
        var picker = w.Picker;
        if (!picker.IsOpen) return;
        picker.Update(w.Assemblies);

        var style = ui.Style;
        var theme = WidgetTheme.For(ui);
        var open = false;
        var backdrop = ui.Interact("picker-backdrop");
        var close = backdrop.Clicked || ui.Input.IsKeyPressed(KeyboardKey.Escape);
        foreach (var stroke in ui.Input.Keys)
        {
            if (stroke.Key == KeyboardKey.UpArrow) picker.Move(-1);
            else if (stroke.Key == KeyboardKey.DownArrow) picker.Move(1);
            else if (stroke.Key == KeyboardKey.PageUp) picker.Move(-10);
            else if (stroke.Key == KeyboardKey.PageDown) picker.Move(10);
        }

        using (ui.Float(0f, 0f, box: new Box
        {
            Width = Sizing.Grow(),
            Height = Sizing.Grow(),
            Background = Scrim,
            AlignX = Align.Center,
            AlignY = Align.Center,
        }, z: 500, id: backdrop.Id))
        using (ui.Column(ui.Interact("picker").Id, new Box
        {
            Width = 560f,
            Height = Sizing.Percent(0.75f),
            Padding = theme.PanelPadding,
            Background = style.Surface,
            BorderColor = style.Border,
            BorderWidth = 1f,
            Radius = theme.CornerRadius,
        }))
        {
            ui.Text("Open from framework", bold: true);
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
                Background = style.Field,
                BorderColor = style.Border,
                BorderWidth = 1f,
                Radius = theme.CornerRadius,
                Padding = 2f,
            }))
            {
                if (picker.IsLoading)
                    ui.Text("Scanning the runtime…", new Box { Padding = 8f }, color: style.TextMuted);
                else
                    open |= Rows(ui, picker);
            }

            using (ui.Row())
            {
                ui.Text(ui.Memo("picker-count", picker.Filtered.Count, static n => $"{n} assemblies"), color: style.TextMuted);
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
        var style = ui.Style;
        var rowHeight = WidgetTheme.For(ui).RowHeight;
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
                var background = selected ? style.Selection : row.Hovered ? style.Hovered(style.Field) : 0u;
                using (ui.Row(row.Id, new Box { Height = rowHeight, Gap = 8f, Padding = new Spacing(8f, 0f), Background = background }))
                {
                    var listed = picker.IsListed(assembly);
                    ui.Text(assembly.Name, color: listed ? style.TextMuted : style.Text);
                    ui.Spacer();
                    ui.Text(listed ? "open" : assembly.Framework, color: style.TextMuted);
                }
            }
        }
        if (picker.ScrollToSelected && list.ScrollIntoView(picker.SelectedIndex))
            picker.ScrollToSelected = false;
        return open;
    }
}
