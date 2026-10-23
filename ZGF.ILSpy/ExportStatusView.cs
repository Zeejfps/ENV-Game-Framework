using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The exports in the status bar: while one runs, what it writes, how far it has got, a
/// button that cancels it and how many wait; once they are done, what they came to, with buttons
/// that open the folder, show the report and dismiss it.</summary>
internal static class ExportStatusView
{
    private const float BarWidth = 72f;

    /// <summary>Draws the running export or the finished ones; returns whether it drew anything.</summary>
    public static bool Render(Ui<UiState> ui, Workspace w)
    {
        var progress = w.Export.Progress;
        if (progress != null)
        {
            Running(ui, w, progress.Value);
            return true;
        }
        if (w.FinishedExports.Count == 0)
            return false;
        Notice(ui, w);
        return true;
    }

    private static void Running(Ui<UiState> ui, Workspace w, ExportProgress progress)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var text = ui.Memo("export-running", (progress.Name, progress.Part, progress.Parts, t), static a => Describe(a.t, a.Name, a.Part, a.Parts));
        ui.Text(text, color: theme.AccentText, size: theme.FontSizeSmall);
        var fraction = progress.Fraction;
        if (fraction != null)
            ui.ProgressBar(fraction.Value, Sizing.Fixed(BarWidth));
        if (IconButton(ui, "cancel-export", LucideIcons.X, t.StatusCancelExport))
            w.Export.Cancel();
        Queued(ui, w);
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

    private static void Notice(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var reports = w.FinishedExports;
        var last = reports[reports.Count - 1];
        var summary = ui.Memo("export-summary", (reports.Count, last, t), static a => ExportReportText.Summarize(a.t, a.Count, a.last));
        ui.Text(OutcomeIcon(reports), color: OutcomeColor(theme, reports), size: theme.IconSize, family: LucideIcons.Family);
        ui.Text(summary, color: theme.Text, size: theme.FontSizeSmall);
        NoticeActions(ui, w, last);
    }

    private static void NoticeActions(Ui<UiState> ui, Workspace w, ExportReport last)
    {
        var t = ui.State.Strings;
        if (last.Outcome != ExportOutcome.Failed && Link(ui, "export-open-folder", t.ExportOpenFolder))
            ui.OpenFolder(last.Folder);
        if (Link(ui, "export-show-report", t.ExportShowReport))
            w.ShowExportReport();
        if (IconButton(ui, "export-dismiss", LucideIcons.X, t.ExportDismiss))
            w.DismissExportNotice();
    }

    private static string OutcomeIcon(List<ExportReport> reports)
    {
        if (reports.Any(static r => r.HasProblems))
            return LucideIcons.AlertCircle;
        return LucideIcons.Check;
    }

    private static uint OutcomeColor(UiTheme theme, List<ExportReport> reports)
    {
        if (reports.Any(static r => r.Outcome == ExportOutcome.Failed))
            return theme.Danger;
        if (reports.Any(static r => r.HasProblems))
            return theme.Warning;
        return theme.Success;
    }

    /// <summary>A text button that fits the status bar's height.</summary>
    private static bool Link(Ui<UiState> ui, string id, string text)
    {
        var theme = ui.Theme;
        var i = ui.Clickable(id);
        using (ui.Row(i.Id, ButtonBox(theme, i)))
            ui.Text(text, color: theme.AccentText, size: theme.FontSizeSmall);
        return i.Clicked;
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
