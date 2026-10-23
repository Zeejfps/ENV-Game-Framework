using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>Every export of the session, as a list of downloads is: the running one with its bar and
/// a cancel button, the waiting ones, which can be taken out of the queue, and the finished ones,
/// newest first, each with its folder and report. Each row keeps its identity, the export's run,
/// whatever state the export is in.</summary>
internal static class ExportsPanelView
{
    private const float PercentWidth = 40f;
    private const int SectionQueuedId = -1;
    private const int SectionFinishedId = -2;

    private static readonly UiIcon OpenFolderIcon = new(LucideIcons.FolderOpen, LucideIcons.Family);
    private static readonly UiIcon ShowReportIcon = new(LucideIcons.FileText, LucideIcons.Family);

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
        var theme = ui.Theme;
        using (ui.PaneToolbar())
        {
            var summary = ui.Memo("summary", (w.Export.IsRunning, w.Export.WaitingCount, w.FinishedExports.Count, t),
                static a => Summarize(a.t, a.IsRunning, a.WaitingCount, a.Count));
            ui.Text(summary, color: theme.TextMuted, size: theme.FontSizeSmall);
            ui.Spacer();
            if (ui.Button(t.ExportsClearFinished, enabled: w.FinishedExports.Count > 0, id: "clear", variant: ButtonVariant.Ghost))
                w.ClearFinishedExports();
            ui.Tooltip(ui.GetId("clear"), t.ExportsClearFinishedTooltip);
        }
    }

    /// <summary>How many exports run, wait and are finished, leaving out what there is none of.</summary>
    private static string Summarize(Strings t, bool running, int waiting, int finished)
    {
        var parts = new List<string>();
        if (running)
            parts.Add(t.ExportsSummaryRunning);
        if (waiting > 0)
            parts.Add(t.ExportsSummaryQueued(waiting));
        if (finished > 0)
            parts.Add(t.ExportsSummaryFinished(finished));
        return string.Join(" · ", parts);
    }

    private static void Running(Ui<UiState> ui, Workspace w)
    {
        var running = w.Export.Running;
        var progress = w.Export.Progress;
        if (running == null || progress == null)
            return;
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Id(running.Run))
        using (ui.Row(RowBox(theme)))
        {
            using (ui.Column(ContentBox()))
            {
                var at = progress.Value;
                var preparing = at.Fraction == null;
                var detail = ui.Memo("detail", (running.Job.Kind, at.Name, at.Part, at.Parts, preparing, w.Export.IsCancelling, t),
                    static a => DescribeRunning(a.t, a.Kind, a.Name, a.Part, a.Parts, a.preparing, a.IsCancelling));
                Title(ui, LucideIcons.Loader2, theme.AccentText, running.Job.Name);
                Detail(ui, detail);
                Folder(ui, running.Job.Folder);
                Bar(ui, progress.Value.Fraction);
            }
            if (ui.Button(t.ExportsCancel, enabled: !w.Export.IsCancelling, id: "cancel", variant: ButtonVariant.Ghost))
                w.Export.Cancel();
            ui.Tooltip(ui.GetId("cancel"), t.ExportsCancelTooltip);
        }
        ui.Separator();
    }

    private static string DescribeRunning(Strings t, ExportKind kind, string name, int part, int parts, bool preparing, bool cancelling)
    {
        var kindText = ExportReportText.KindOf(t, kind);
        if (cancelling)
            return $"{kindText} · {t.ExportsCancelling}";
        if (parts > 1)
            return $"{kindText} · {t.ExportsAssemblyPart(part, parts, name)}";
        if (preparing)
            return $"{kindText} · {t.ExportsPreparing}";
        return kindText;
    }

    /// <summary>The bar of the running export, with its percentage beside it in a slot of its own so
    /// the bar keeps its length; empty while the export does not know its size yet.</summary>
    private static void Bar(Ui<UiState> ui, float? fraction)
    {
        var theme = ui.Theme;
        using (ui.Row(new Box { Width = Sizing.Grow(), Gap = theme.Gap, AlignY = Align.Center }))
        {
            ui.ProgressBar(fraction ?? 0f);
            var percent = ui.Memo("percent", fraction, static f => FormatPercent(f));
            ui.Text(percent, new Box { Width = Sizing.Fixed(PercentWidth) }, color: theme.TextMuted, size: theme.FontSizeSmall, align: Align.End);
        }
    }

    private static string FormatPercent(float? fraction)
    {
        if (fraction == null)
            return "";
        var percent = (int)(fraction.Value * 100f);
        return $"{percent}%";
    }

    private static void Waiting(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var count = w.Export.WaitingCount;
        if (count > 1)
            Section(ui, SectionQueuedId, ui.Memo("queued", (count, t), static a => a.t.ExportsSectionQueued(a.count)));
        var ahead = 0;
        foreach (var waiting in w.Export.Waiting)
        {
            WaitingRow(ui, w, waiting, ahead);
            ahead++;
        }
    }

    private static void WaitingRow(Ui<UiState> ui, Workspace w, QueuedExport waiting, int ahead)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Id(waiting.Run))
        using (ui.Row(RowBox(theme)))
        {
            using (ui.Column(ContentBox()))
            {
                var detail = ui.Memo("detail", (waiting.Job.Kind, ahead, t), static a => DescribeWaiting(a.t, a.Kind, a.ahead));
                Title(ui, LucideIcons.Hourglass, theme.TextMuted, waiting.Job.Name);
                Detail(ui, detail);
                Folder(ui, waiting.Job.Folder);
            }
            if (ui.Button(t.ExportsWithdraw, id: "withdraw", variant: ButtonVariant.Ghost))
                w.Export.Withdraw(waiting.Run);
        }
        ui.Separator();
    }

    private static string DescribeWaiting(Strings t, ExportKind kind, int ahead)
    {
        var kindText = ExportReportText.KindOf(t, kind);
        if (ahead == 0)
            return $"{kindText} · {t.ExportsUpNext}";
        return $"{kindText} · {t.ExportsAhead(ahead)}";
    }

    private static void Finished(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var reports = w.FinishedExports;
        if (reports.Count == 0)
            return;
        if (HasActiveWork(w))
            Section(ui, SectionFinishedId, ui.Memo("finished", (reports.Count, t), static a => a.t.ExportsSectionFinished(a.Count)));
        ExportReport? removed = null;
        for (var i = reports.Count - 1; i >= 0; i--)
        {
            if (FinishedRow(ui, w, reports[i]))
                removed = reports[i];
        }
        if (removed != null)
            w.RemoveFinishedExport(removed);
    }

    private static bool HasActiveWork(Workspace w)
    {
        if (w.Export.IsRunning)
            return true;
        return w.Export.WaitingCount > 0;
    }

    /// <summary>Draws a finished export; returns whether the user took it off the list.</summary>
    private static bool FinishedRow(Ui<UiState> ui, Workspace w, ExportReport report)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        bool removed;
        using (ui.Id(report.Run))
        using (ui.Row(RowBox(theme)))
        {
            using (ui.Column(ContentBox()))
            {
                var detail = ui.Memo("detail", (report, t), static a => DescribeFinished(a.t, a.report));
                Title(ui, OutcomeIcon(report), OutcomeColor(theme, report), report.Name);
                Detail(ui, detail, OutcomeTextColor(theme, report));
                Folder(ui, report.Folder);
            }
            FinishedActions(ui, w, report);
            removed = ui.IconButton("remove", LucideIcons.X, LucideIcons.Family, t.ExportsRemove);
        }
        ui.Separator();
        return removed;
    }

    private static string DescribeFinished(Strings t, ExportReport report)
    {
        var kindText = ExportReportText.KindOf(t, report.Job.Kind);
        var outcome = ExportReportText.DescribeOutcome(t, report);
        return $"{kindText} · {outcome}";
    }

    /// <summary>One labelled action that suits how the export ended, and the other as an icon:
    /// the folder after a clean finish, the report after problems, a failure or a cancel.</summary>
    private static void FinishedActions(Ui<UiState> ui, Workspace w, ExportReport report)
    {
        var t = ui.State.Strings;
        if (OpensFolderFirst(report))
        {
            if (ui.Button(t.ExportOpenFolder, id: "open-folder", icon: OpenFolderIcon))
                ui.OpenFolder(report.Folder);
            if (ui.IconButton("show-report", LucideIcons.FileText, LucideIcons.Family, t.ExportShowReport))
                w.ShowExportReport(report);
            return;
        }
        if (ui.Button(t.ExportShowReport, id: "show-report", icon: ShowReportIcon))
            w.ShowExportReport(report);
        if (report.FolderExists && ui.IconButton("open-folder", LucideIcons.FolderOpen, LucideIcons.Family, t.ExportOpenFolder))
            ui.OpenFolder(report.Folder);
    }

    private static bool OpensFolderFirst(ExportReport report)
    {
        if (report.Outcome != ExportOutcome.Finished)
            return false;
        if (report.HasProblems)
            return false;
        return report.FolderExists;
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

    /// <summary>The color of the line about how the export ended: the outcome's own when something
    /// went wrong, so the problem reads as well as the icon shows it; muted otherwise.</summary>
    private static uint OutcomeTextColor(UiTheme theme, ExportReport report)
    {
        if (report.HasProblems)
            return OutcomeColor(theme, report);
        return theme.TextMuted;
    }

    /// <summary>A muted heading over a group of rows, with how many it holds.</summary>
    private static void Section(Ui<UiState> ui, int id, string text)
    {
        var theme = ui.Theme;
        using (ui.Id(id))
        using (ui.Row(new Box { Width = Sizing.Grow(), Padding = new Spacing(theme.PanelPadding, theme.GapTight), Background = theme.Surface }))
            ui.Text(text, color: theme.TextMuted, size: theme.FontSizeSmall, bold: true);
        ui.Separator();
    }

    /// <summary>The export's icon and name on one line, the icon in line with the name.</summary>
    private static void Title(Ui<UiState> ui, string glyph, uint color, string name)
    {
        var theme = ui.Theme;
        using (ui.Row(new Box { Width = Sizing.Grow(), Gap = theme.Gap, AlignY = Align.Center }))
        {
            ui.Text(glyph, color: color, size: theme.IconSize, family: LucideIcons.Family);
            ui.Text(name, new Box { Width = Sizing.Grow() }, color: theme.Text, overflow: TextOverflow.Ellipsis);
        }
    }

    /// <summary>A muted line under the name, in line with it.</summary>
    private static void Detail(Ui<UiState> ui, string text, uint? color = null)
    {
        var theme = ui.Theme;
        ui.Text(text, new Box { Width = Sizing.Grow(), Padding = Indent(theme) }, color: color ?? theme.TextMuted, size: theme.FontSizeSmall,
            overflow: TextOverflow.Ellipsis);
    }

    /// <summary>The folder the export writes into, cut in the middle so its last folder stays, and
    /// whole in a tooltip.</summary>
    private static void Folder(Ui<UiState> ui, string folder)
    {
        var theme = ui.Theme;
        var hover = ui.Interact(ui.GetId("folder"));
        using (ui.Row(hover.Id, new Box { Width = Sizing.Grow(), Padding = Indent(theme) }))
            ui.Text(folder, new Box { Width = Sizing.Grow() }, color: theme.TextMuted, size: theme.FontSizeSmall, overflow: TextOverflow.EllipsisMiddle);
        ui.Tooltip(hover.Id, folder);
    }

    private static Spacing Indent(UiTheme theme) => new(theme.IconSize + theme.Gap, 0f, 0f, 0f);

    private static Box RowBox(UiTheme theme) => new()
    {
        Width = Sizing.Grow(),
        Gap = theme.Gap,
        Padding = new Spacing(theme.PanelPadding, theme.Gap),
        AlignY = Align.Center,
    };

    private static Box ContentBox() => new() { Width = Sizing.Grow(), Gap = 0f };
}
