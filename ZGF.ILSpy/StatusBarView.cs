using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The shown node's full name at the left, ellipsized, and a content-sized cluster at the
/// right: activity, errors, language and the assembly count.</summary>
internal static class StatusBarView
{
    public static void Render(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var size = theme.FontSizeSmall;
        using (ui.Row(new Box
        {
            Height = theme.StatusBarHeight,
            Padding = new Spacing(theme.ShellPaddingX, 0f),
            Gap = theme.GroupGap,
            AlignY = Align.Center,
            Background = theme.Surface,
        }))
        {
            var selected = w.Tabs.Active?.Node;
            ui.Text(ui.Memo("status-node", selected, static n => n?.FullName ?? ""), new Box { Width = Sizing.Grow() }, color: theme.TextMuted,
                size: size, overflow: TextOverflow.Ellipsis);

            using (ui.Row(new Box { Width = Sizing.Fit(), Height = Sizing.Grow(), Gap = theme.GapTight, AlignY = Align.Center }))
            {
                var progress = w.Export.Progress;
                if (progress != null)
                {
                    ExportStatus(ui, w, progress.Value);
                    Dot(ui);
                }
                var activity = DescribeActivity(w, t);
                if (activity != null)
                {
                    ui.Text(activity, color: theme.AccentText, size: size);
                    Dot(ui);
                }
                if (w.Errors.Entries.Count > 0)
                {
                    ErrorIndicator(ui, w);
                    Dot(ui);
                }
                ui.Text(w.Language == DecompilationLanguage.CSharp ? "C#" : "IL", color: theme.TextMuted, size: size);
                Dot(ui);
                ui.Text(ui.Memo("status-count", (w.Tree.Roots.Count, t), static a => a.t.StatusAssemblies(a.Count)), color: theme.TextMuted, size: size);
            }
        }
    }

    private static string? DescribeActivity(Workspace w, Strings t)
    {
        if (w.IsReloading)
            return t.StatusReloading;
        if (IsDecompiling(w))
            return t.StatusDecompiling;
        if (w.IsResolving)
            return t.StatusResolving;
        return null;
    }

    private const float ExportBarWidth = 72f;

    /// <summary>What is being exported, how far it has got, and the button that cancels it.</summary>
    private static void ExportStatus(Ui<UiState> ui, Workspace w, ExportProgress progress)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var text = ui.Memo("status-export", (progress.Name, progress.Part, progress.Parts, t), static a => DescribeExport(a.t, a.Name, a.Part, a.Parts));
        ui.Text(text, color: theme.AccentText, size: theme.FontSizeSmall);
        var fraction = progress.Fraction;
        if (fraction != null)
            ui.ProgressBar(fraction.Value, Sizing.Fixed(ExportBarWidth));
        CancelExportButton(ui, w);
        QueuedExports(ui, w);
    }

    private static void QueuedExports(Ui<UiState> ui, Workspace w)
    {
        var waiting = w.Export.WaitingCount;
        if (waiting == 0)
            return;
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var text = ui.Memo("status-export-queued", (waiting, t), static a => a.t.StatusExportQueued(a.waiting));
        ui.Text(text, color: theme.TextMuted, size: theme.FontSizeSmall);
    }

    private static string DescribeExport(Strings t, string name, int part, int parts)
    {
        if (parts > 1)
            return t.StatusExportingPart(part, parts, name);
        return t.StatusExportingName(name);
    }

    private static void CancelExportButton(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var i = ui.Clickable("cancel-export");
        if (i.Clicked)
            w.Export.Cancel();

        using (ui.Row(i.Id, new Box
        {
            Width = Sizing.Fit(),
            Height = Sizing.Grow(),
            Padding = new Spacing(theme.GapTight, 0f),
            AlignY = Align.Center,
            Background = i.Held ? theme.Pressed(theme.RowHover) : i.Hovered ? theme.RowHover : 0u,
        }))
        {
            ui.Text(LucideIcons.X, color: theme.TextMuted, size: theme.IconSize, family: LucideIcons.Family);
        }
        ui.Tooltip(i.Id, t.StatusCancelExport);
    }

    private static bool IsDecompiling(Workspace w)
    {
        var tab = w.Tabs.Active;
        return tab != null && tab.IsBusy;
    }

    private static void Dot(Ui<UiState> ui) => ui.Text("·", color: ui.Theme.TextDisabled, size: ui.Theme.FontSizeSmall);

    private static void ErrorIndicator(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var errors = w.Errors;
        var i = ui.Clickable("errors");
        if (i.Clicked)
            errors.Open();

        using (ui.Row(i.Id, new Box
        {
            Width = Sizing.Fit(),
            Height = Sizing.Grow(),
            Gap = theme.GapTight,
            Padding = new Spacing(theme.GapTight, 0f),
            AlignY = Align.Center,
            Background = i.Held ? theme.Pressed(theme.DangerSurface) : i.Hovered ? theme.DangerSurface : 0u,
        }))
        {
            ui.Text(LucideIcons.AlertCircle, color: theme.Danger, size: theme.IconSize, family: LucideIcons.Family);
            ui.Text(ui.Memo("status-errors", (errors.Entries.Count, t), static a => a.t.StatusErrors(a.Count)), color: theme.Danger,
                size: theme.FontSizeSmall);
        }
        ui.Tooltip(i.Id, errors.Entries[^1].Summary);
    }
}
