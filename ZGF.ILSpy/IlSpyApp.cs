using ZGF.AppUtils;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

public sealed class IlSpyApp : Gui<Workspace>
{
    private static readonly (string Family, string File)[] Fonts =
    [
        (MonoFonts.Regular, "JetBrainsMono-Regular.ttf"),
        (MonoFonts.Bold, "JetBrainsMono-Bold.ttf"),
        (MonoFonts.Italic, "JetBrainsMono-Italic.ttf"),
        (MonoFonts.BoldItalic, "JetBrainsMono-BoldItalic.ttf"),
        (LucideIcons.Family, LucideIcons.File),
    ];

    public IlSpyApp(Workspace workspace) : base(workspace)
    {
        foreach (var (family, file) in Fonts)
            RegisterFont(family, EmbeddedAssets.LoadBytes(typeof(IlSpyApp).Assembly, file));
        workspace.Attach(Post);
        workspace.Assemblies.Changed += Invalidate;
    }

    protected override void Render(Ui ui, Workspace w) => MainView.Render(ui, w, Strings.For(w.Locale));
}
