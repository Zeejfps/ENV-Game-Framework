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
        if (!errors.IsOpen)
            return;
        var theme = ui.Theme;
        var title = ui.Memo("errors-title", (errors.Entries.Count, t), static a => a.t.ErrorsTitle(a.Count));
        using var dialog = ui.Dialog("errors", title, ref errors.IsOpen, new Box
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

        using (ui.Panel(new Box { Height = Sizing.Grow(), Padding = theme.BorderWidth, Background = theme.EditorBackground, Clip = true }))
            ui.CodeView("error-text", errors.SelectedDocument(), errors.View, t);

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
        var entries = errors.Entries;
        using var list = ui.SelectableList(ui.GetId("error-list"), entries.Count, errors.Selection, box: new Box
        {
            Height = Math.Min(entries.Count, AppLayout.ErrorListRows) * theme.RowHeight,
            Background = theme.Field,
        });
        for (var i = list.First; i < list.End; i++)
        {
            var entry = entries[i];
            using (ui.Id(i))
            using (var row = list.Row(i, ui.GetId("row"), new Box { Gap = theme.Gap }))
            {
                var titleColor = row.Selected ? theme.SelectionText : theme.Text;
                ui.Text(ui.Memo("time", (entry.Time, t), static a => a.Time.ToString("T", a.t.Culture)), color: theme.TextMuted);
                ui.Text(entry.Title, new Box { Width = Sizing.Grow() }, color: titleColor, overflow: TextOverflow.Ellipsis);
            }
        }
    }
}
