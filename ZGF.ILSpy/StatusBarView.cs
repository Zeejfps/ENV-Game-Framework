using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The shown node's full name at the left, ellipsized, and a content-sized cluster at the
/// right: activity, errors, language and the assembly count.</summary>
internal static class StatusBarView
{
    public static void Render(Ui ui, Workspace w, Strings t)
    {
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
            var selected = w.ActiveTab?.Node;
            ui.Text(ui.Memo("status-node", selected, static n => n?.FullName ?? ""), new Box { Width = Sizing.Grow() }, color: theme.TextMuted,
                size: size, overflow: TextOverflow.Ellipsis);

            using (ui.Row(new Box { Width = Sizing.Fit(), Height = Sizing.Grow(), Gap = theme.GapTight, AlignY = Align.Center }))
            {
                var activity = w.IsReloading ? t.StatusReloading : w.ActiveTab is { IsBusy: true } ? t.StatusDecompiling : w.IsResolving ? t.StatusResolving : null;
                if (activity != null)
                {
                    ui.Text(activity, color: theme.AccentText, size: size);
                    Dot(ui);
                }
                if (w.Errors.Entries.Count > 0)
                {
                    ErrorIndicator(ui, w, t);
                    Dot(ui);
                }
                ui.Text(w.Language == DecompilationLanguage.CSharp ? "C#" : "IL", color: theme.TextMuted, size: size);
                Dot(ui);
                ui.Text(ui.Memo("status-count", (w.Tree.Roots.Count, t), static a => a.t.StatusAssemblies(a.Count)), color: theme.TextMuted, size: size);
            }
        }
    }

    private static void Dot(Ui ui) => ui.Text("·", color: ui.Theme.TextDisabled, size: ui.Theme.FontSizeSmall);

    private static void ErrorIndicator(Ui ui, Workspace w, Strings t)
    {
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
