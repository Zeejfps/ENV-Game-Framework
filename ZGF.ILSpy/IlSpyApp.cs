using ZGF.AppUtils;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy;

public sealed class IlSpyApp : Gui<Workspace>
{
    private static readonly (string Family, string File)[] Fonts =
    [
        (MonoFonts.Regular, "JetBrainsMono-Regular.ttf"),
        (MonoFonts.Bold, "JetBrainsMono-Bold.ttf"),
        (MonoFonts.Italic, "JetBrainsMono-Italic.ttf"),
        (MonoFonts.BoldItalic, "JetBrainsMono-BoldItalic.ttf"),
    ];

    public IlSpyApp(Workspace workspace) : base(workspace)
    {
        foreach (var (family, file) in Fonts)
            RegisterFont(family, EmbeddedAssets.LoadBytes(typeof(IlSpyApp).Assembly, file));
        workspace.Attach(Post);
        workspace.Assemblies.Changed += Invalidate;
    }

    protected override void Render(Ui ui, Workspace w)
    {
        using var screen = ui.Screen(w.DarkMode ? AppThemes.Dark : AppThemes.Light);
        w.Sync();
        Shortcuts.Handle(ui, w);
        using (ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f }))
        {
            MainMenuView.Render(ui, w);
            ToolbarView.Render(ui, w);
            ui.Separator();
            using (var split = ui.SplitPane("main", Direction.Horizontal, ref w.TreeWidth, minFirst: 160f, minSecond: 240f))
            {
                using (split.First())
                    TreePaneView.Render(ui, w);
                using (split.Second())
                    ContentPane(ui, w);
            }
            ui.Separator();
            StatusBarView.Render(ui, w);
        }
        FrameworkPickerView.Render(ui, w);
    }

    private static void ContentPane(Ui ui, Workspace w)
    {
        var wait = w.Search.Update(w.Assemblies, ui.RealTime, w.Post);
        if (wait > 0)
            ui.RequestFrameIn(wait);

        if (!w.Search.IsOpen)
        {
            DocumentsView.Render(ui, w);
            return;
        }

        using var split = ui.SplitPane("search-split", Direction.Vertical, ref w.SearchHeight, minFirst: 120f, minSecond: 80f, sized: SplitSide.Second);
        using (split.First())
            DocumentsView.Render(ui, w);
        using (split.Second())
            SearchPanelView.Render(ui, w);
    }
}
