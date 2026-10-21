using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

internal static class StatusBarView
{
    public static void Render(Ui ui, Workspace w)
    {
        var style = ui.Style;
        using (ui.Row(new Box { Height = AppStyles.BarHeight, Padding = new Spacing(10f, 0f), Gap = 16f, Background = style.Surface }))
        {
            var selected = w.ActiveTab?.Node;
            ui.Text(ui.Memo("status-node", selected, static n => n?.FullName ?? ""), color: style.TextMuted);
            ui.Leaf(new Box { Width = Sizing.Grow(), Height = 0f });

            var activity = w.ActiveTab is { IsBusy: true } ? "Decompiling…" : w.IsResolving ? "Resolving reference…" : null;
            if (activity != null)
                ui.Text(activity, color: style.Accent);
            else if (w.Message != null)
                ui.Text(w.Message, color: style.TextMuted);

            var language = w.Language == DecompilationLanguage.CSharp ? "C#" : "IL";
            ui.Text(language, color: style.TextMuted);
            ui.Text(ui.Memo("status-count", w.Tree.Roots.Count, static n => n == 1 ? "1 assembly" : $"{n} assemblies"), color: style.TextMuted);
        }
    }
}
