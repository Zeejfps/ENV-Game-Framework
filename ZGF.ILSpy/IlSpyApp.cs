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

    private IReadOnlyList<string>? _startup;

    /// <param name="startup">Files to open once the window is up.</param>
    public IlSpyApp(Workspace workspace, IReadOnlyList<string> startup) : base(workspace)
    {
        _startup = startup;
        foreach (var (family, file) in Fonts)
            RegisterFont(family, EmbeddedAssets.LoadBytes(typeof(IlSpyApp).Assembly, file));
        workspace.Attach(Post);
        workspace.Assemblies.Changed += Invalidate;
    }

    protected override void Render(Ui ui, Workspace w)
    {
        if (_startup != null)
        {
            w.Open(_startup);
            _startup = null;
        }
        MainView.Render(ui, w);
    }
}
