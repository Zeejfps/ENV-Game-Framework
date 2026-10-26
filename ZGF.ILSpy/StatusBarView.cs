using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The shown node's full name at the left, ellipsized, and a content-sized cluster at the
/// right: activity, errors, language and the assembly count.</summary>
internal static class StatusBarView
{
    public static void Render(Ui<UiState> ui, AppState appState)
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
            var selected = appState.Tabs.Active?.Node;
            ui.Text(ui.Memo("status-node", selected, static n => n?.FullName ?? ""), new Box { Width = Sizing.Grow() }, color: theme.TextMuted,
                size: size, overflow: TextOverflow.Ellipsis);

            using (ui.Row(new Box { Width = Sizing.Fit(), Height = Sizing.Grow(), Gap = theme.GapTight, AlignY = Align.Center }))
            {
                if (ExportStatusView.Render(ui, appState))
                    Dot(ui);
                var activity = DescribeActivity(appState, t);
                if (activity != null)
                {
                    ui.Text(activity, color: theme.AccentText, size: size);
                    Dot(ui);
                }
                if (appState.Errors.Entries.Count > 0)
                {
                    ErrorIndicator(ui, appState);
                    Dot(ui);
                }
                ui.Text(DecompilationLanguages.Label(appState.Language), color: theme.TextMuted, size: size);
                Dot(ui);
                ui.Text(ui.Memo("status-count", (ui.State.Tree.Roots.Count, t), static a => a.t.StatusAssemblies(a.Count)), color: theme.TextMuted, size: size);
            }
        }
    }

    private static string? DescribeActivity(AppState appState, Strings t)
    {
        if (appState.IsReloading)
            return t.StatusReloading;
        if (appState.IsReopening)
            return t.StatusReloading;
        if (IsDecompiling(appState))
            return t.StatusDecompiling;
        if (appState.IsResolving)
            return t.StatusResolving;
        return null;
    }

    private static bool IsDecompiling(AppState appState)
    {
        var tab = appState.Tabs.Active;
        return tab != null && tab.IsBusy;
    }

    private static void Dot(Ui<UiState> ui) => ui.Text("·", color: ui.Theme.TextDisabled, size: ui.Theme.FontSizeSmall);

    private static void ErrorIndicator(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var errors = appState.Errors;
        var i = ui.Clickable("errors");
        if (i.Clicked)
            ui.State.ErrorDialog.Open(errors.Entries);

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
            ui.Glyph(ui.State.Icons.StatusError, theme.Danger, theme.IconSize);
            ui.Text(ui.Memo("status-errors", (errors.Entries.Count, t), static a => a.t.StatusErrors(a.Count)), color: theme.Danger,
                size: theme.FontSizeSmall);
        }
        ui.Tooltip(i.Id, errors.Entries[^1].Summary);
    }
}
