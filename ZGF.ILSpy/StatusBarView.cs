using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

internal static class StatusBarView
{
    private const float BadgeSize = 14f;

    public static void Render(Ui ui, Workspace w)
    {
        var theme = ui.Theme;
        using (ui.Row(new Box { Height = AppThemes.BarHeight, Padding = new Spacing(10f, 0f), Gap = 16f, Background = theme.Surface }))
        {
            using (ui.Box(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), AlignY = Align.Center, Clip = true }))
            {
                var selected = w.ActiveTab?.Node;
                ui.Text(ui.Memo("status-node", selected, static n => n?.FullName ?? ""), color: theme.TextMuted);
            }

            var activity = w.IsReloading ? "Reloading…" : w.ActiveTab is { IsBusy: true } ? "Decompiling…" : w.IsResolving ? "Resolving…" : null;
            if (activity != null)
                ui.Text(activity, color: theme.Accent);
            if (w.Errors.Entries.Count > 0)
                ErrorIndicator(ui, w);

            ui.Text(w.Language == DecompilationLanguage.CSharp ? "C#" : "IL", color: theme.TextMuted);
            ui.Text(ui.Memo("status-count", w.Tree.Roots.Count, static n => n == 1 ? "1 assembly" : $"{n} assemblies"), color: theme.TextMuted);
        }
    }

    private static void ErrorIndicator(Ui ui, Workspace w)
    {
        var theme = ui.Theme;
        var errors = w.Errors;
        var color = CodeViewPalette.For(theme).Error;
        var i = ui.Interact("errors");
        if (i.Hovered)
            ui.SetCursor(MouseCursor.Hand);
        if (i.Clicked)
            errors.Open();

        using (ui.Row(i.Id, new Box
        {
            Height = Sizing.Grow(),
            Gap = 6f,
            Padding = new Spacing(6f, 0f),
            AlignY = Align.Center,
            Background = i.Hovered ? theme.Hovered(theme.Surface) : 0u,
        }))
        {
            ui.Text("!", new Box { Width = BadgeSize, Height = BadgeSize, Radius = BadgeSize / 2, Background = color }, color: theme.OnAccent,
                bold: true, align: Align.Center, size: MathF.Round(theme.FontSize * 0.85f));
            ui.Text(ui.Memo("status-errors", errors.Entries.Count, static n => n == 1 ? "1 error" : $"{n} errors"), color: color);
        }
        ui.Tooltip(i.Id, errors.Entries[^1].Summary);
    }
}
