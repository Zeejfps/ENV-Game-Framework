using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

internal static class StatusBarView
{
    public static void Render(Ui ui, Workspace w)
    {
        var theme = ui.Theme;
        using (ui.Row(new Box { Height = AppThemes.BarHeight, Padding = new Spacing(10f, 0f), Gap = 16f, Background = theme.Surface }))
        {
            var selected = w.ActiveTab?.Node;
            ui.Text(ui.Memo("status-node", selected, static n => n?.FullName ?? ""), color: theme.TextMuted);
            ui.Leaf(new Box { Width = Sizing.Grow(), Height = 0f });

            var activity = w.ActiveTab is { IsBusy: true } ? "Decompiling…" : w.IsResolving ? "Resolving reference…" : null;
            if (activity != null)
                ui.Text(activity, color: theme.Accent);
            else if (w.Message != null)
                ui.Text(w.Message, color: theme.TextMuted);

            var language = w.Language == DecompilationLanguage.CSharp ? "C#" : "IL";
            ui.Text(language, color: theme.TextMuted);
            ui.Text(ui.Memo("status-count", w.Tree.Roots.Count, static n => n == 1 ? "1 assembly" : $"{n} assemblies"), color: theme.TextMuted);
        }
    }
}
