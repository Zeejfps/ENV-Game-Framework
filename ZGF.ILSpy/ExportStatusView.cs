using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The running export in the status bar, for a glance: what it writes and how far it has
/// got, which opens the exports panel when clicked, a button that cancels it, and how many wait.</summary>
internal static class ExportStatusView
{
    private const float BarWidth = 72f;

    /// <summary>Draws the running export; returns whether one runs.</summary>
    public static bool Render(Ui<UiState> ui, Workspace w)
    {
        var progress = w.Export.Progress;
        if (progress == null)
            return false;
        Running(ui, w, progress.Value);
        return true;
    }

    private static void Running(Ui<UiState> ui, Workspace w, ExportProgress progress)
    {
        var t = ui.State.Strings;
        if (Glance(ui, progress))
            w.OpenExports();
        if (IconButton(ui, "cancel-export", LucideIcons.X, t.StatusCancelExport))
            w.Export.Cancel();
        Queued(ui, w);
    }

    /// <summary>What the export writes and its bar, as one button; returns whether it was clicked.</summary>
    private static bool Glance(Ui<UiState> ui, ExportProgress progress)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var i = ui.Clickable("export-glance");
        using (ui.Row(i.Id, ButtonBox(theme, i)))
        {
            var text = ui.Memo("export-running", (progress.Name, progress.Part, progress.Parts, t), static a => Describe(a.t, a.Name, a.Part, a.Parts));
            ui.Text(text, color: theme.AccentText, size: theme.FontSizeSmall);
            var fraction = progress.Fraction;
            if (fraction != null)
                ui.ProgressBar(fraction.Value, Sizing.Fixed(BarWidth));
        }
        ui.Tooltip(i.Id, t.StatusShowExports);
        return i.Clicked;
    }

    private static string Describe(Strings t, string name, int part, int parts)
    {
        if (parts > 1)
            return t.StatusExportingPart(part, parts, name);
        return t.StatusExportingName(name);
    }

    private static void Queued(Ui<UiState> ui, Workspace w)
    {
        var waiting = w.Export.WaitingCount;
        if (waiting == 0)
            return;
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var text = ui.Memo("export-queued", (waiting, t), static a => a.t.StatusExportQueued(a.waiting));
        ui.Text(text, color: theme.TextMuted, size: theme.FontSizeSmall);
    }

    /// <summary>An icon button that fits the status bar's height, which the theme's are too tall for.</summary>
    private static bool IconButton(Ui<UiState> ui, string id, string glyph, string tooltip)
    {
        var theme = ui.Theme;
        var i = ui.Clickable(id);
        using (ui.Row(i.Id, ButtonBox(theme, i)))
            ui.Text(glyph, color: theme.TextMuted, size: theme.IconSize, family: LucideIcons.Family);
        ui.Tooltip(i.Id, tooltip);
        return i.Clicked;
    }

    private static Box ButtonBox(UiTheme theme, Interaction i)
    {
        return new Box
        {
            Width = Sizing.Fit(),
            Height = Sizing.Grow(),
            Gap = theme.GapTight,
            Padding = new Spacing(theme.GapTight, 0f),
            AlignY = Align.Center,
            Background = HoverBackground(theme, i),
        };
    }

    private static uint HoverBackground(UiTheme theme, Interaction i)
    {
        if (i.Held)
            return theme.Pressed(theme.RowHover);
        if (i.Hovered)
            return theme.RowHover;
        return 0u;
    }
}
