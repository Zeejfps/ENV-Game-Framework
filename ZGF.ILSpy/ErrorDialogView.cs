using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy;

/// <summary>The recent errors: a list to pick one from when there are several, its full text in a
/// selectable monospace view, and Copy, Clear and Close.</summary>
internal static class ErrorDialogView
{
    private const int ListRows = 5;

    public static void Render(Ui ui, Workspace w)
    {
        var errors = w.Errors;
        var theme = ui.Theme;
        using var dialog = ui.Dialog("errors", errors.Entries.Count == 1 ? "Error" : "Errors", ref errors.IsOpen, new Box { Width = 760f, Height = 480f });
        if (!dialog.IsOpen) return;

        if (errors.Entries.Count > 1)
            List(ui, errors);

        using (ui.Box(new Box
        {
            Width = Sizing.Grow(),
            Height = Sizing.Grow(),
            BorderColor = theme.Border,
            BorderWidth = 1f,
            Radius = theme.CornerRadius,
            Clip = true,
        }))
        {
            ui.CodeView("error-text", errors.SelectedDocument(), errors.View);
        }

        using (ui.Row())
        {
            if (ui.Button("Copy", id: "error-copy") && errors.SelectedError is { } error)
                ui.ClipboardText = error.FullText;
            ui.Tooltip(ui.GetId("error-copy"), "Copy the whole error");
            if (ui.Button("Clear", id: "error-clear"))
                errors.Clear();
            ui.Spacer();
            if (ui.Button("Close", id: "error-close"))
                errors.IsOpen = false;
        }
    }

    private static void List(Ui ui, ErrorLog errors)
    {
        var theme = ui.Theme;
        var rowHeight = theme.RowHeight;
        var entries = errors.Entries;
        using var list = ui.VirtualScroll("error-list", entries.Count, rowHeight, new Box
        {
            Height = Math.Min(entries.Count, ListRows) * rowHeight,
            Gap = 0f,
            Background = theme.Field,
        });
        for (var i = list.First; i < list.End; i++)
        {
            var entry = entries[i];
            using (ui.Id(i))
            {
                var row = ui.Interact("row");
                if (row.Pressed)
                    errors.Select(i);
                var background = i == errors.Selected ? theme.Selection : row.Hovered ? theme.Hovered(theme.Field) : 0u;
                using (ui.Row(row.Id, new Box { Width = Sizing.Grow(), Height = rowHeight, Gap = 10f, Padding = new Spacing(8f, 0f), Background = background, Clip = true }))
                {
                    ui.Text(ui.Memo("time", entry.Time, static t => t.ToString("T")), color: theme.TextMuted);
                    ui.Text(entry.Title);
                }
            }
        }
    }
}
