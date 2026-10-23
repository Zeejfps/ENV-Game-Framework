using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>Every export of the session, as a list of downloads is: the running one with its bar and
/// a cancel button, the waiting ones, which can be taken out of the queue, and the finished ones,
/// newest first, each with its folder and report.</summary>
internal static class ExportsPanelView
{
    private const int RunningId = 0;
    private const int WaitingIds = 1 << 20;
    private const int FinishedIds = 2 << 20;

    public static void Render(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
        Toolbar(ui, w);
        ui.Separator();
        if (IsEmpty(w))
        {
            DocumentsView.Hint(ui, t.ExportsHint);
            return;
        }
        using (ui.Scroll("exports", new Box { Height = Sizing.Grow(), Gap = 0f, Background = ui.Theme.PaneBackground }))
        {
            Running(ui, w);
            Waiting(ui, w);
            Finished(ui, w);
        }
    }

    private static bool IsEmpty(Workspace w)
    {
        if (w.Export.IsRunning)
            return false;
        return w.FinishedExports.Count == 0;
    }

    private static void Toolbar(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        using (ui.PaneToolbar())
        {
            if (ui.IconButton("clear", LucideIcons.ListX, LucideIcons.Family, t.ExportsClearFinished, w.FinishedExports.Count > 0))
                w.ClearFinishedExports();
        }
    }

    private static void Running(Ui<UiState> ui, Workspace w)
    {
        var progress = w.Export.Progress;
        if (progress == null)
            return;
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Id(RunningId))
        using (ui.Column(RowBox(theme)))
        {
            using (ui.Row(LineBox(theme)))
            {
                Icon(ui, LucideIcons.Loader2, theme.AccentText);
                var detail = ui.Memo("detail", (progress.Value.Name, progress.Value.Part, progress.Value.Parts, t), static a => DescribePart(a.t, a.Name, a.Part, a.Parts));
                Titles(ui, w.Export.RunningName ?? "", detail);
                if (ui.Button(t.ExportsCancel, id: "cancel", variant: ButtonVariant.Ghost))
                    w.Export.Cancel();
            }
            var fraction = progress.Value.Fraction;
            if (fraction != null)
                ui.ProgressBar(fraction.Value);
        }
        ui.Separator();
    }

    private static string DescribePart(Strings t, string name, int part, int parts)
    {
        if (parts > 1)
            return t.StatusExportingPart(part, parts, name);
        return t.StatusExportingName(name);
    }

    private static void Waiting(Ui<UiState> ui, Workspace w)
    {
        foreach (var waiting in w.Export.Waiting)
            WaitingRow(ui, w, waiting);
    }

    private static void WaitingRow(Ui<UiState> ui, Workspace w, QueuedExport waiting)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Id(WaitingIds + waiting.Run))
        using (ui.Column(RowBox(theme)))
        using (ui.Row(LineBox(theme)))
        {
            Icon(ui, LucideIcons.Hourglass, theme.TextMuted);
            Titles(ui, waiting.Name, t.ExportsWaiting);
            if (ui.Button(t.ExportsWithdraw, id: "withdraw", variant: ButtonVariant.Ghost))
                w.Export.Withdraw(waiting.Run);
        }
        ui.Separator();
    }

    private static void Finished(Ui<UiState> ui, Workspace w)
    {
        var reports = w.FinishedExports;
        ExportReport? removed = null;
        for (var i = reports.Count - 1; i >= 0; i--)
        {
            if (FinishedRow(ui, w, reports[i], i))
                removed = reports[i];
        }
        if (removed != null)
            w.RemoveFinishedExport(removed);
    }

    /// <summary>Draws a finished export; returns whether the user took it off the list.</summary>
    private static bool FinishedRow(Ui<UiState> ui, Workspace w, ExportReport report, int index)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var removed = false;
        using (ui.Id(FinishedIds + index))
        using (ui.Column(RowBox(theme)))
        {
            using (ui.Row(LineBox(theme)))
            {
                Icon(ui, OutcomeIcon(report), OutcomeColor(theme, report));
                var outcome = ui.Memo("outcome", (report, t), static a => ExportReportText.DescribeOutcome(a.t, a.report));
                Titles(ui, report.Name, outcome);
                FinishedActions(ui, w, report);
                removed = ui.IconButton("remove", LucideIcons.X, LucideIcons.Family, t.ExportsRemove);
            }
            ui.Text(report.Folder, new Box { Width = Sizing.Grow(), Padding = new Spacing(IconIndent(theme), 0f, 0f, 0f) }, color: theme.TextMuted,
                size: theme.FontSizeSmall, overflow: TextOverflow.Ellipsis);
        }
        ui.Separator();
        return removed;
    }

    private static void FinishedActions(Ui<UiState> ui, Workspace w, ExportReport report)
    {
        var t = ui.State.Strings;
        if (report.Outcome != ExportOutcome.Failed && ui.Button(t.ExportOpenFolder, id: "open-folder", variant: ButtonVariant.Ghost))
            ui.OpenFolder(report.Folder);
        if (ui.Button(t.ExportShowReport, id: "show-report", variant: ButtonVariant.Ghost))
            w.ShowExportReport(report);
    }

    private static string OutcomeIcon(ExportReport report)
    {
        if (report.HasProblems)
            return LucideIcons.AlertCircle;
        if (report.Outcome == ExportOutcome.Cancelled)
            return LucideIcons.X;
        return LucideIcons.Check;
    }

    private static uint OutcomeColor(UiTheme theme, ExportReport report)
    {
        if (report.Outcome == ExportOutcome.Failed)
            return theme.Danger;
        if (report.HasProblems)
            return theme.Warning;
        if (report.Outcome == ExportOutcome.Cancelled)
            return theme.TextMuted;
        return theme.Success;
    }

    private static void Icon(Ui<UiState> ui, string glyph, uint color)
    {
        var theme = ui.Theme;
        ui.Text(glyph, color: color, size: theme.IconSize, family: LucideIcons.Family);
    }

    /// <summary>The export's name over a muted line about it, taking the row's spare width.</summary>
    private static void Titles(Ui<UiState> ui, string title, string detail)
    {
        var theme = ui.Theme;
        using (ui.Column(new Box { Width = Sizing.Grow(), Gap = 0f }))
        {
            ui.Text(title, new Box { Width = Sizing.Grow() }, color: theme.Text, overflow: TextOverflow.Ellipsis);
            ui.Text(detail, new Box { Width = Sizing.Grow() }, color: theme.TextMuted, size: theme.FontSizeSmall, overflow: TextOverflow.Ellipsis);
        }
    }

    private static float IconIndent(UiTheme theme) => theme.IconSize + theme.Gap;

    private static Box RowBox(UiTheme theme) => new() { Width = Sizing.Grow(), Gap = theme.GapTight, Padding = new Spacing(theme.PanelPadding, theme.Gap) };

    private static Box LineBox(UiTheme theme) => new() { Width = Sizing.Grow(), Gap = theme.Gap, AlignY = Align.Center };
}
