using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The recent errors: a list to pick one from when there are several, the picked one's
/// title over its details in a selectable monospace view, and Copy, Clear and Close.</summary>
internal static class ErrorDialogView
{
    public static void Render(Ui ui, Workspace w, Strings t)
    {
        var errors = w.Errors;
        var theme = ui.Theme;
        using var dialog = ui.Dialog("errors", t.ErrorsTitle(errors.Entries.Count), ref errors.IsOpen, new Box
        {
            Width = Sizing.Grow(0f, AppLayout.ErrorDialogWidth),
            Height = Sizing.Grow(0f, AppLayout.ErrorDialogHeight),
        });
        if (!dialog.IsOpen) return;

        if (errors.Entries.Count > 1)
            List(ui, errors, t);

        if (errors.SelectedError is { } selected)
        {
            using (ui.Row(new Box { Gap = theme.GapTight, AlignY = Align.Center }))
            {
                ui.Text(LucideIcons.AlertCircle, color: theme.Danger, size: theme.IconSize, family: LucideIcons.Family);
                ui.Text(selected.Title, new Box { Width = Sizing.Grow() }, bold: true, overflow: TextOverflow.Ellipsis);
            }
        }

        using (ui.Box(new Box
        {
            Width = Sizing.Grow(),
            Height = Sizing.Grow(),
            Padding = theme.BorderWidth,
            BorderColor = theme.Border,
            BorderWidth = theme.BorderWidth,
            Radius = theme.PanelRadius,
            Background = theme.EditorBackground,
            Clip = true,
        }))
        {
            ui.CodeView("error-text", errors.SelectedDocument(), errors.View, t);
        }

        using (ui.Row(new Box { Gap = theme.GapTight }))
        {
            if (ui.Button(t.ErrorsCopy, id: "error-copy") && errors.SelectedError is { } error)
                ui.ClipboardText = error.FullText;
            ui.Tooltip(ui.GetId("error-copy"), t.ErrorsCopyTooltip);
            if (ui.Button(t.ErrorsClear, id: "error-clear", variant: ButtonVariant.Ghost))
                errors.Clear();
            ui.Spacer();
            if (ui.Button(t.ErrorsClose, id: "error-close", variant: ButtonVariant.Primary))
                errors.IsOpen = false;
        }
    }

    private static void List(Ui ui, ErrorLog errors, Strings t)
    {
        var theme = ui.Theme;
        var rowHeight = theme.RowHeight;
        var entries = errors.Entries;
        using var list = ui.VirtualScroll("error-list", entries.Count, rowHeight, new Box
        {
            Height = Math.Min(entries.Count, AppLayout.ErrorListRows) * rowHeight,
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
                var selected = i == errors.Selected;
                var background = selected ? theme.Selection : row.Hovered ? theme.RowHover : 0u;
                using (ui.Row(row.Id, new Box
                {
                    Width = Sizing.Grow(),
                    Height = rowHeight,
                    Gap = theme.Gap,
                    Padding = new Spacing(theme.ShellPaddingX, 0f),
                    Background = background,
                }))
                {
                    ui.Text(ui.Memo("time", (entry.Time, t), static a => a.Time.ToString("T", a.t.Culture)), color: theme.TextMuted);
                    ui.Text(entry.Title, new Box { Width = Sizing.Grow() }, color: selected ? theme.SelectionText : theme.Text, overflow: TextOverflow.Ellipsis);
                }
            }
        }
    }
}
