using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy;

/// <summary>Every export of the session, as a list of downloads is: the running one with its bar and
/// a cancel button, the waiting ones, which can be taken out of the queue, and the finished ones,
/// newest first, each with its folder and report. A row is selected by clicking it or with the
/// arrow keys; Enter or a double-click opens a finished export's report, Delete takes a row off,
/// Ctrl+C copies its folder, and right-click or Shift+F10 opens its menu. Each row keeps its
/// identity, the export's run, whatever state the export is in. Narrower than
/// <see cref="NarrowWidth"/>, the actions become icons.</summary>
internal static class ExportsPanelView
{
    private const string ListKey = "exports";
    private const string MenuId = "export-menu";
    private const float NarrowWidth = 400f;
    private const float PercentWidth = 40f;
    private const int SectionQueuedId = -1;
    private const int SectionFinishedId = -2;
    private const int PageRows = 5;
    private const double AnimationStep = 1.0 / 30.0;
    private const double SpinnerPeriod = 0.9;
    private const double SlidePeriod = 1.4;
    private const int SpinnerDots = 8;
    private const float SlideShare = 0.3f;

    private static readonly UiIcon OpenFolderIcon = new(LucideIcons.FolderOpen, LucideIcons.Family);
    private static readonly UiIcon ShowReportIcon = new(LucideIcons.FileText, LucideIcons.Family);

    /// <summary>What every row of a frame needs: the list it is in, for focus and scrolling, the
    /// menu its rows open, resolved outside their id scopes, and whether the panel is narrow.</summary>
    private readonly record struct Rows(int ListId, int MenuId, bool Narrow);

    public static void Render(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
        Toolbar(ui, appState);
        ui.Separator();
        if (IsEmpty(appState))
        {
            DocumentsView.Hint(ui, t.ExportsHint);
            return;
        }
        var listId = ui.GetId(ListKey);
        var rows = new Rows(listId, ui.GetId(MenuId), IsNarrow(ui, listId));
        HandleKeys(ui, appState, rows);
        using (ui.Scroll(rows.ListId, new Box { Height = Sizing.Grow(), Gap = 0f, Background = ui.Theme.PaneBackground }))
        {
            Running(ui, appState, rows);
            Waiting(ui, appState, rows);
            Finished(ui, appState, rows);
        }
        Menu(ui, appState, rows);
        KeepAnimating(ui, appState);
    }

    private static bool IsEmpty(AppState appState)
    {
        if (appState.Export.IsRunning)
            return false;
        return appState.FinishedExports.Count == 0;
    }

    /// <summary>Whether the list was narrower than <see cref="NarrowWidth"/> on the last frame.</summary>
    private static bool IsNarrow(Ui<UiState> ui, int listId)
    {
        if (!ui.TryGetRect(listId, out var rect))
            return false;
        return rect.Width < NarrowWidth;
    }

    private static void Toolbar(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.PaneToolbar(new Box { Padding = new Spacing(theme.PanelPadding, theme.ToolbarPaddingY) }))
        {
            var summary = ui.Memo("summary", (appState.Export.IsRunning, appState.Export.WaitingCount, appState.FinishedExports.Count, t),
                static a => Summarize(a.t, a.IsRunning, a.WaitingCount, a.Count));
            ui.Text(summary, color: theme.TextMuted, size: theme.FontSizeSmall);
            ui.Spacer();
            if (ui.Button(t.ExportsClearFinished, enabled: appState.FinishedExports.Count > 0, id: "clear", variant: ButtonVariant.Ghost))
                Exporting.ClearFinishedExports(appState);
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

    /// <summary>Walks and acts on the selection with the keys pressed this frame, while the list has focus.</summary>
    private static void HandleKeys(Ui<UiState> ui, AppState appState, Rows rows)
    {
        if (!ui.HasFocus(rows.ListId))
            return;
        var runs = Exporting.ListExports(appState);
        foreach (var stroke in ui.Input.Keys)
            HandleKey(ui, appState, rows, runs, stroke);
    }

    private static void HandleKey(Ui<UiState> ui, AppState appState, Rows rows, List<int> runs, KeyStroke stroke)
    {
        var selected = ui.State.SelectedExport;
        if (IsMenuKey(stroke))
        {
            OpenMenuOnSelection(ui, appState, rows, selected);
            return;
        }
        if (stroke.Key == KeyboardKey.Enter)
        {
            Exporting.ActivateExport(appState, selected);
            return;
        }
        if (IsCopy(stroke))
        {
            CopyFolder(ui, appState, selected);
            return;
        }
        if (stroke.Key == KeyboardKey.Delete)
        {
            Exporting.RemoveExport(appState, selected);
            return;
        }
        MoveSelection(ui, appState, rows, runs, stroke);
    }

    private static bool IsMenuKey(KeyStroke stroke)
    {
        if (stroke.Key == KeyboardKey.Menu)
            return true;
        return stroke.Key == KeyboardKey.F10 && stroke.Modifiers.HasFlag(KeyModifiers.Shift);
    }

    private static bool IsCopy(KeyStroke stroke)
    {
        if (stroke.Key != KeyboardKey.C)
            return false;
        return stroke.Modifiers.HasFlag(KeyModifiers.Control);
    }

    private static void MoveSelection(Ui<UiState> ui, AppState appState, Rows rows, List<int> runs, KeyStroke stroke)
    {
        var index = runs.IndexOf(ui.State.SelectedExport);
        var before = index;
        UiWidgets.ApplyListKey(stroke, ref index, runs.Count, PageRows);
        if (index == before || index < 0)
            return;
        ui.State.SelectedExport = runs[index];
        using (ui.Id(ui.State.SelectedExport))
            ui.ScrollIntoView(rows.ListId, ui.GetId("row"));
    }

    private static void CopyFolder(Ui<UiState> ui, AppState appState, int run)
    {
        var folder = Exporting.FolderOfExport(appState, run);
        if (folder == null)
            return;
        ui.ClipboardText = folder;
    }

    private static void OpenMenuOnSelection(Ui<UiState> ui, AppState appState, Rows rows, int run)
    {
        if (run == 0)
            return;
        PointF? at = null;
        using (ui.Id(run))
        {
            if (ui.TryGetRect(ui.GetId("row"), out var rect))
                at = new PointF(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        }
        OpenMenu(ui, appState, rows, run, at);
    }

    private static void OpenMenu(Ui<UiState> ui, AppState appState, Rows rows, int run, PointF? at)
    {
        ui.State.ExportMenuTarget = run;
        ui.OpenContextMenu(rows.MenuId, at);
    }

    /// <summary>The interaction of a whole row: a press selects it and focuses the list, a
    /// double-click activates it, a right-click opens its menu.</summary>
    private static Interaction RowHit(Ui<UiState> ui, AppState appState, Rows rows, int run)
    {
        var hit = ui.Interact(ui.GetId("row"));
        if (hit.Pressed)
        {
            ui.State.SelectedExport = run;
            ui.Focus(rows.ListId, takesText: false);
        }
        if (hit.DoubleClicked)
            Exporting.ActivateExport(appState, run);
        if (hit.ClickedWith(PointerButton.Right))
        {
            ui.State.SelectedExport = run;
            OpenMenu(ui, appState, rows, run, ui.Input.Pointer);
        }
        return hit;
    }

    private static uint RowBackground(Ui<UiState> ui, AppState appState, Rows rows, int run, Interaction hit)
    {
        var theme = ui.Theme;
        if (ui.State.SelectedExport == run)
            return ui.HasFocus(rows.ListId) ? theme.Selection : theme.SelectionInactive;
        if (hit.Hovered)
            return theme.RowHover;
        return 0u;
    }

    private static void Running(Ui<UiState> ui, AppState appState, Rows rows)
    {
        var running = appState.Export.Running;
        var progress = appState.Export.Progress;
        if (running == null || progress == null)
            return;
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Id(running.Run))
        {
            var hit = RowHit(ui, appState, rows, running.Run);
            using (ui.Column(hit.Id, RowBox(theme, RowBackground(ui, appState, rows, running.Run, hit))))
            {
                var at = progress.Value;
                using (ui.Row(TopLine(theme)))
                {
                    Spinner(ui);
                    Name(ui, running.Job.Name);
                    Percent(ui, at.Fraction);
                    CancelAction(ui, appState, rows);
                }
                var preparing = at.Fraction == null;
                var detail = ui.Memo("detail", (running.Job.Kind, at.Name, at.Part, at.Parts, preparing, appState.Export.IsCancelling, t),
                    static a => DescribeRunning(a.t, a.Kind, a.Name, a.Part, a.Parts, a.preparing, a.IsCancelling));
                Detail(ui, detail);
                Folder(ui, running.Job.Folder);
                Bar(ui, at.Fraction);
            }
            ui.Tooltip(hit.Id, running.Job.Folder);
        }
        ui.Separator();
    }

    private static void CancelAction(Ui<UiState> ui, AppState appState, Rows rows)
    {
        var t = ui.State.Strings;
        var enabled = !appState.Export.IsCancelling;
        if (rows.Narrow)
        {
            if (ui.IconButton("cancel", LucideIcons.X, LucideIcons.Family, t.ExportsCancelTooltip, enabled))
                appState.Export.Cancel();
            return;
        }
        if (ui.Button(t.ExportsCancel, enabled: enabled, id: "cancel", variant: ButtonVariant.Ghost))
            appState.Export.Cancel();
        ui.Tooltip(ui.GetId("cancel"), t.ExportsCancelTooltip);
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


    /// <summary>Dots in a ring, the brightest going round once every <see cref="SpinnerPeriod"/>.</summary>
    private static void Spinner(Ui<UiState> ui)
    {
        var theme = ui.Theme;
        var phase = (float)(ui.RealTime % SpinnerPeriod / SpinnerPeriod);
        using (ui.Box(new Box { Width = Sizing.Fixed(theme.IconSize), Height = Sizing.Fixed(theme.IconSize) }))
            ui.Paint((phase, theme.AccentText, theme.PaneBackground), static (p, s) => PaintSpinner(p, s.phase, s.AccentText, s.PaneBackground));
    }

    private static void PaintSpinner(UiPainter p, float phase, uint color, uint background)
    {
        var r = p.Rect;
        var center = new PointF(r.X + r.Width / 2, r.Y + r.Height / 2);
        var ring = r.Width * 0.38f;
        var dot = r.Width * 0.09f;
        for (var i = 0; i < SpinnerDots; i++)
        {
            var share = (float)i / SpinnerDots;
            var angle = share * MathF.Tau;
            var at = new PointF(center.X + MathF.Cos(angle) * ring, center.Y + MathF.Sin(angle) * ring);
            var behind = (phase - share + 1f) % 1f;
            var shade = UiColor.Mix(color, background, behind * 0.85f);
            p.Circle(at, dot, shade);
        }
    }

    /// <summary>How much of the running export is done, on its top line in a slot of its own so the
    /// actions beside it stay put; empty while the export does not know its size.</summary>
    private static void Percent(Ui<UiState> ui, float? fraction)
    {
        var theme = ui.Theme;
        var percent = ui.Memo("percent", fraction, static f => FormatPercent(f));
        ui.Text(percent, new Box { Width = Sizing.Fixed(PercentWidth) }, color: theme.TextMuted, size: theme.FontSizeSmall, align: Align.End);
    }

    /// <summary>The bar of the running export, from the name's indent to the row's right edge; a
    /// segment slides along it while the export does not know its size.</summary>
    private static void Bar(Ui<UiState> ui, float? fraction)
    {
        var theme = ui.Theme;
        using (ui.Row(new Box { Width = Sizing.Grow(), Padding = new Spacing(NameIndent(theme), theme.GapTight, 0f, 0f) }))
        {
            if (fraction != null)
                ui.ProgressBar(fraction.Value);
            else
                SlidingBar(ui);
        }
    }

    private static void SlidingBar(Ui<UiState> ui)
    {
        var theme = ui.Theme;
        var height = theme.ProgressHeight;
        var phase = (float)(ui.RealTime % SlidePeriod / SlidePeriod);
        using (ui.Box(new Box { Width = Sizing.Grow(), Height = height, Background = theme.Control, Radius = height / 2 }))
            ui.Paint((phase, theme.Accent), static (p, s) => PaintSlide(p, s.phase, s.Accent));
    }

    private static void PaintSlide(UiPainter p, float phase, uint color)
    {
        var r = p.Rect;
        var width = r.Width * SlideShare;
        var travel = r.Width + width;
        var left = r.X - width + travel * phase;
        var start = MathF.Max(r.X, left);
        var end = MathF.Min(r.X + r.Width, left + width);
        if (end <= start)
            return;
        p.FillRect(new UiRect(start, r.Y, end - start, r.Height), color, r.Height / 2);
    }

    private static string FormatPercent(float? fraction)
    {
        if (fraction == null)
            return "";
        var percent = (int)(fraction.Value * 100f);
        return $"{percent}%";
    }

    private static void Waiting(Ui<UiState> ui, AppState appState, Rows rows)
    {
        var t = ui.State.Strings;
        var count = appState.Export.WaitingCount;
        if (count > 1)
            Section(ui, SectionQueuedId, ui.Memo("queued", (count, t), static a => a.t.ExportsSectionQueued(a.count)));
        var ahead = 0;
        var withdrawn = 0;
        foreach (var waiting in appState.Export.Waiting)
        {
            if (WaitingRow(ui, appState, rows, waiting, ahead))
                withdrawn = waiting.Run;
            ahead++;
        }
        if (withdrawn != 0)
            appState.Export.Withdraw(withdrawn);
    }

    /// <summary>Draws a waiting export; returns whether the user took it out of the queue, which
    /// the caller does once the queue is no longer being walked.</summary>
    private static bool WaitingRow(Ui<UiState> ui, AppState appState, Rows rows, QueuedExport waiting, int ahead)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        bool withdrawn;
        using (ui.Id(waiting.Run))
        {
            var hit = RowHit(ui, appState, rows, waiting.Run);
            using (ui.Column(hit.Id, RowBox(theme, RowBackground(ui, appState, rows, waiting.Run, hit))))
            {
                using (ui.Row(TopLine(theme)))
                {
                    Glyph(ui, LucideIcons.Hourglass, theme.TextMuted);
                    Name(ui, waiting.Job.Name);
                    withdrawn = WithdrawAction(ui, rows);
                }
                var detail = ui.Memo("detail", (waiting.Job.Kind, ahead, t), static a => DescribeWaiting(a.t, a.Kind, a.ahead));
                Detail(ui, detail);
                Folder(ui, waiting.Job.Folder);
            }
            ui.Tooltip(hit.Id, waiting.Job.Folder);
        }
        ui.Separator();
        return withdrawn;
    }

    /// <summary>The button that takes a waiting export out of the queue; returns whether it was clicked.</summary>
    private static bool WithdrawAction(Ui<UiState> ui, Rows rows)
    {
        var t = ui.State.Strings;
        if (rows.Narrow)
            return ui.IconButton("withdraw", LucideIcons.X, LucideIcons.Family, t.ExportsWithdraw);
        return ui.Button(t.ExportsWithdraw, id: "withdraw", variant: ButtonVariant.Ghost);
    }

    private static string DescribeWaiting(Strings t, ExportKind kind, int ahead)
    {
        var kindText = ExportReportText.KindOf(t, kind);
        if (ahead == 0)
            return $"{kindText} · {t.ExportsUpNext}";
        return $"{kindText} · {t.ExportsAhead(ahead)}";
    }

    private static void Finished(Ui<UiState> ui, AppState appState, Rows rows)
    {
        var t = ui.State.Strings;
        var reports = appState.FinishedExports;
        if (reports.Count == 0)
            return;
        if (HasActiveWork(appState))
            Section(ui, SectionFinishedId, ui.Memo("finished", (reports.Count, t), static a => a.t.ExportsSectionFinished(a.Count)));
        ExportReport? removed = null;
        for (var i = reports.Count - 1; i >= 0; i--)
        {
            if (FinishedRow(ui, appState, rows, reports[i]))
                removed = reports[i];
        }
        if (removed != null)
            Exporting.RemoveFinishedExport(appState, removed);
    }

    private static bool HasActiveWork(AppState appState)
    {
        if (appState.Export.IsRunning)
            return true;
        return appState.Export.WaitingCount > 0;
    }

    /// <summary>Draws a finished export; returns whether the user took it off the list.</summary>
    private static bool FinishedRow(Ui<UiState> ui, AppState appState, Rows rows, ExportReport report)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        bool removed;
        using (ui.Id(report.Run))
        {
            var hit = RowHit(ui, appState, rows, report.Run);
            using (ui.Column(hit.Id, RowBox(theme, RowBackground(ui, appState, rows, report.Run, hit))))
            {
                using (ui.Row(TopLine(theme)))
                {
                    Glyph(ui, OutcomeIcon(report), OutcomeColor(theme, report));
                    Name(ui, report.Name);
                    FinishedActions(ui, appState, rows, report);
                    removed = ui.IconButton("remove", LucideIcons.X, LucideIcons.Family, t.ExportsRemove);
                }
                var detail = ui.Memo("detail", (report, t), static a => DescribeFinished(a.t, a.report));
                Detail(ui, detail, OutcomeTextColor(theme, report));
                Folder(ui, report.Folder);
            }
            ui.Tooltip(hit.Id, report.Folder);
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
    /// the folder after a clean finish, the report after problems, a failure or a cancel. Both are
    /// icons when the panel is narrow.</summary>
    private static void FinishedActions(Ui<UiState> ui, AppState appState, Rows rows, ExportReport report)
    {
        if (OpensFolderFirst(report))
        {
            OpenFolderAction(ui, report, labelled: !rows.Narrow);
            ShowReportAction(ui, appState, report, labelled: false);
            return;
        }
        ShowReportAction(ui, appState, report, labelled: !rows.Narrow);
        if (report.FolderExists)
            OpenFolderAction(ui, report, labelled: false);
    }

    private static void OpenFolderAction(Ui<UiState> ui, ExportReport report, bool labelled)
    {
        var t = ui.State.Strings;
        if (labelled && ui.Button(t.ExportOpenFolder, id: "open-folder", icon: OpenFolderIcon))
            ui.OpenFolder(report.Folder);
        if (!labelled && ui.IconButton("open-folder", LucideIcons.FolderOpen, LucideIcons.Family, t.ExportOpenFolder))
            ui.OpenFolder(report.Folder);
    }

    private static void ShowReportAction(Ui<UiState> ui, AppState appState, ExportReport report, bool labelled)
    {
        var t = ui.State.Strings;
        if (labelled && ui.Button(t.ExportShowReport, id: "show-report", icon: ShowReportIcon))
            Exporting.ShowExportReport(appState, report);
        if (!labelled && ui.IconButton("show-report", LucideIcons.FileText, LucideIcons.Family, t.ExportShowReport))
            Exporting.ShowExportReport(appState, report);
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

    /// <summary>The menu of the export it was opened on, with what can be done to it where it is.</summary>
    private static void Menu(Ui<UiState> ui, AppState appState, Rows rows)
    {
        using var menu = ui.ContextMenu(rows.MenuId);
        if (!menu.IsOpen)
            return;
        var run = ui.State.ExportMenuTarget;
        var finished = Exporting.FindFinishedExport(appState, run);
        if (finished != null)
            FinishedMenu(ui, appState, finished);
        else
            QueuedMenu(ui, appState, run);
    }

    private static void FinishedMenu(Ui<UiState> ui, AppState appState, ExportReport report)
    {
        var t = ui.State.Strings;
        if (ui.MenuItem(t.ExportShowReport, "Enter", id: "show-report", icon: ShowReportIcon))
            Exporting.ShowExportReport(appState, report);
        if (ui.MenuItem(t.ExportOpenFolder, enabled: report.FolderExists, id: "open-folder", icon: OpenFolderIcon))
            ui.OpenFolder(report.Folder);
        if (ui.MenuItem(t.ExportsCopyFolder, "Ctrl+C", id: "copy-folder"))
            ui.ClipboardText = report.Folder;
        ui.MenuSeparator();
        if (ui.MenuItem(t.ExportsRemove, "Delete", id: "remove"))
            Exporting.RemoveFinishedExport(appState, report);
    }

    private static void QueuedMenu(Ui<UiState> ui, AppState appState, int run)
    {
        var t = ui.State.Strings;
        var folder = Exporting.FolderOfExport(appState, run);
        if (folder == null)
            return;
        if (ui.MenuItem(t.ExportsCopyFolder, "Ctrl+C", id: "copy-folder"))
            ui.ClipboardText = folder;
        ui.MenuSeparator();
        if (appState.Export.Running?.Run == run)
        {
            if (ui.MenuItem(t.ExportsCancel, enabled: !appState.Export.IsCancelling, id: "cancel"))
                appState.Export.Cancel();
            return;
        }
        if (ui.MenuItem(t.ExportsWithdraw, "Delete", id: "withdraw"))
            appState.Export.Withdraw(run);
    }

    /// <summary>Asks for the next frame soon while an export runs, so its spinner and sliding bar move.</summary>
    private static void KeepAnimating(Ui<UiState> ui, AppState appState)
    {
        if (!appState.Export.IsRunning)
            return;
        ui.RequestUpdateIn(AnimationStep);
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

    private static void Glyph(Ui<UiState> ui, string glyph, uint color)
    {
        var theme = ui.Theme;
        ui.Text(glyph, new Box { Width = Sizing.Fixed(theme.IconSize) }, color: color, size: theme.IconSize, family: LucideIcons.Family);
    }

    /// <summary>The export's name, taking the top line's spare width and cut short when it runs out.</summary>
    private static void Name(Ui<UiState> ui, string name)
    {
        var theme = ui.Theme;
        ui.Text(name, new Box { Width = Sizing.Grow() }, color: theme.Text, overflow: TextOverflow.Ellipsis);
    }

    /// <summary>A muted line under the name, in line with it.</summary>
    private static void Detail(Ui<UiState> ui, string text, uint? color = null)
    {
        var theme = ui.Theme;
        ui.Text(text, new Box { Width = Sizing.Grow(), Padding = Indent(theme) }, color: color ?? theme.TextMuted, size: theme.FontSizeSmall,
            overflow: TextOverflow.Ellipsis);
    }

    /// <summary>The folder the export writes into, cut in the middle so its last folder stays; the
    /// row's tooltip holds it whole.</summary>
    private static void Folder(Ui<UiState> ui, string folder)
    {
        var theme = ui.Theme;
        ui.Text(folder, new Box { Width = Sizing.Grow(), Padding = Indent(theme) }, color: theme.TextMuted, size: theme.FontSizeSmall,
            overflow: TextOverflow.EllipsisMiddle);
    }

    /// <summary>Where the name and every line under it start: past the icon and the gap after it.</summary>
    private static float NameIndent(UiTheme theme) => theme.IconSize + theme.Gap;

    private static Spacing Indent(UiTheme theme) => new(NameIndent(theme), 0f, 0f, 0f);

    /// <summary>A row: its top line, then the lines under it, all within the panel's padding.</summary>
    private static Box RowBox(UiTheme theme, uint background) => new()
    {
        Width = Sizing.Grow(),
        Gap = 0f,
        Padding = new Spacing(theme.PanelPadding, theme.GapTight, theme.PanelPadding, theme.Gap),
        Background = background,
    };

    /// <summary>The row's top line: icon, name and actions, all centered on it.</summary>
    private static Box TopLine(UiTheme theme) => new() { Width = Sizing.Grow(), Gap = theme.Gap, AlignY = Align.Center };
}
